using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class GameST : MonoBehaviourPunCallbacks
{
    int myID;
    PhotonView pv;
    public Canvas canvas;
    Deck deck;
    int playerNum;
    int arriveNum;
    int readyPlayerNum;
    List<GameObject> cards; // 場にあるカード
    List<int> cards_num; // 場にあるカードのID
    int total; // 場にアルカードの合計
    public GameObject[] names = new GameObject[7];
    public GameObject[] hands = new GameObject[7];
    int turn; // 手番のプレイヤーID
    int precall, pprecall; // 1つ前のコールナンバー, 2つ前のコールナンバー
    int cantcall; // コール不可なナンバー
    public Button button0, button1;
    public List<Button> items = new List<Button>(6);
    public InputField callNumTF;
    public GameObject nextButton, backButton;
    int addNum; // ?カードによって追加されたカードID(-1: ?カードなし)
    int addNumPlayerID; // ?カードを持つプレイヤーのID(-1: ?カードなし)
    int[] exceptNums = new int[6] { 10, 15, 20, -5, -10, 0 }; // カードID 6～11
    public GameObject[] blowoffs = new GameObject[7];
    List<int> lives; // プレイヤー毎のライフ
    List<bool> survives; // プレイヤーの生死
    public List<GameObject> life_images = new List<GameObject>(14);
    bool item_double, item_reverse, item_draw1, item_shot;
    bool room_same, room_double, room_reverse, room_draw1, room_shot;
    bool itemUsed;
    bool selectName; // アイテム"DRAW1", "SHOT"のプレイヤー選択状態
    int selectID; // アイテム"DRAW1", "SHOT"の選択プレイヤーID
    int shotPreID; // "SHOT"前のプレイヤーID
    int[] randomIDs; // ランダムID

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        playerNum = (int)getRoomProperty("Num");
        myID = PlayerPrefs.GetInt(PhotonNetwork.NickName + "ID");
        lives = new List<int>();
        survives = new List<bool>();
        cards = new List<GameObject>();
        if (PlayerPrefs.GetInt(PhotonNetwork.NickName + "midway") == 1)
        {
            // ホストにplayerNum, survivesを送ってもらう。
            pv.RPC("SendMidway", RpcTarget.MasterClient);
            return;
        }
        turn = 1;
        for (int i = 0; i < playerNum; i++)
        {
            lives.Add(2);
            survives.Add(true);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            arriveNum = playerNum;
            cards_num = new List<int>();
            addNum = -1;
            addNumPlayerID = -1;
            deck = GetComponent<Deck>();
            readyPlayerNum = 0;
        }
        else
        {
            pv.RPC("Ready", RpcTarget.MasterClient, "SRID");
        }

        /*
        else
        {
            pv.RPC("ReadyAll", RpcTarget.MasterClient, "Setup");
        }
        */
    }

    [PunRPC]
    void Setup()
    {
        Display();
        DisplayLives();
        item_double = false;
        item_reverse = false;
        room_same = false;
        itemUsed = false;
        selectName = false;
        selectID = -1;
        if (PhotonNetwork.IsMasterClient)
        {
            AddRoomProperty("start", 1); // 1: ゲーム開始済み
        }
        else
        {
            // マスタークライアントにDealをしてもらう。
            pv.RPC("Ready", RpcTarget.MasterClient, "Deal");
        }
    }

    // (ホスト専用)myIDをランダムIDでセットし直し、他のプレイヤーにもセットさせる。
    [PunRPC]
    void SRID()
    {
        randomIDs = new int[playerNum];
        for (int j = 0; j < playerNum; j++)
        {
            randomIDs[j] = j + 1;
        }
        Shuffle(randomIDs);
        myID = randomIDs[myID - 1];
        pv.RPC("SetRandomID", RpcTarget.Others, randomIDs);
    }

    // (ホスト以外専用)myIDをランダムIDでセットし直し、Setupの準備を送る。
    [PunRPC]
    void SetRandomID(int[] rIDs)
    {
        myID = rIDs[myID - 1];
        randomIDs = rIDs;
        pv.RPC("ReadyAll", RpcTarget.MasterClient, "Setup");
    }

    // (ホスト専用)カード配布・表示させる。
    [PunRPC]
    void Deal()
    {
        List<int> players = new List<int>();
        for (int i = 0; i < playerNum; i++)
        {
            players.Add(i);
        }
        // 各プレイヤーに配るカードを決定し、ルームプロパティに追加する。
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        for (int k = 0; k < playerNum; k++)
        {
            int j = UnityEngine.Random.Range(0, players.Count);
            int i = players[j];
            players.RemoveAt(j);
            if (survives[i] == false) // 死プレイヤーはスルー
            {
                continue;
            }
            object x = null;
            int n = deck.GetCards();
            if (properties.TryGetValue("card" + (i + 1), out x))
            {
                properties["card" + (i + 1)] = n;
            }
            else
            {
                properties.Add("card" + (i + 1), n);
            }
            cards_num.Add(n);

            // ?カードの時、カードを追加する。
            if (n == 14)
            {
                int m = deck.GetCards();
                cards_num.Add(m);
                addNum = m;
                addNumPlayerID = i + 1;
            }
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
        Calculate();

        // 全プレイヤーにルームプロパティに基づいてカードを表示する関数を送る。
        pv.RPC("DisplayCard", RpcTarget.All);
        precall = 0;
        cantcall = 0;
        room_reverse = false;
        room_draw1 = false;
        room_shot = false;
    }

    // (ホスト専用)全プレイヤーに手番のIDプレイヤーのターン開始を命令する。
    [PunRPC]
    void Turn()
    {
        pv.RPC("MyTurn", RpcTarget.All, turn, precall, room_same, room_draw1, cantcall);
    }

    // (ホスト専用)プレイヤーがnをコールした。
    [PunRPC]
    void SetCallNum(int n, bool dob, bool r, bool dra, bool sho, int id)
    {
        pprecall = precall;
        precall = n;
        cantcall = 0;
        room_same = false;
        room_double = dob;
        if (r)
        {
            room_reverse = !room_reverse;
        }
        else if (dra)
        {
            Draw1(id);
            room_draw1 = true;
        }
        room_shot = sho;
        pv.RPC("OffBlowoff", RpcTarget.All);
        pv.RPC("SetBlowoff", RpcTarget.All, turn, ""+n);
        if (sho)
        {
            shotPreID = turn;
            turn = id;
        }
        else
        {
            NextTurn(true);
        }
        Turn();
    }

    // (ホスト専用)プレイヤーがコヨーテした。
    [PunRPC]
    void Coyote()
    {
        pv.RPC("OpenMyCard", RpcTarget.All);
        if (addNum != -1)
        {
            pv.RPC("AddDisplayCard", RpcTarget.All, addNum, addNumPlayerID, false);
            addNum = -1;
            addNumPlayerID = -1;
        }
        pv.RPC("SetBlowoff", RpcTarget.All, turn, "コヨーテ");
        if (total < precall) // コヨーテ成功
        {
            PreTurn(true); // 手番をコール失敗者まで遡る。
        }
        lives[turn - 1] -= 1;
        int life = lives[turn - 1];
        pv.RPC("LessLife", RpcTarget.All, turn, life);
        // DOUBLE適用時は更に1ライフ減らす。
        if (room_double)
        {
            lives[turn - 1] -= 1;
            if (lives[turn - 1] > -2)
            {
                life = lives[turn - 1];
                pv.RPC("LessLife", RpcTarget.All, turn, life);
            }
        }
        // ライフを減らしたプレイヤーが死んだ場合、その次のプレイヤーが手番になる。
        if (life == -1)
        {
            arriveNum -= 1;
            NextTurn(true);
        }
        deck.ToDiscards();

        // 生プレイヤーを数え、表示ボタンを決定。
        if (arriveNum > 1) // 生プレイヤーが2人以上
        {
            nextButton.SetActive(true); // 次ゲーム開始ボタン表示。
        }
        else
        {
            backButton.SetActive(true); // 全ゲーム終了ボタン表示。
        }
    }

    // (ホスト専用)プレイヤーがSAMEした。
    [PunRPC]
    void Same()
    {
        room_same = true;
        room_double = false;
        pv.RPC("SetBlowoff", RpcTarget.All, turn, "SAME");
        NextTurn(true);
        Turn();
    }

    void Draw1(int id)
    {
        int n = deck.GetCards();
        cards_num.Add(n);
        Calculate();
        pv.RPC("AddDisplayCard", RpcTarget.All, n, id, true);
    }

    // (ホスト専用)プレイヤーがCHANGEした。
    [PunRPC]
    void Change()
    {
        pv.RPC("SetBlowoff", RpcTarget.All, turn, "CHANGE");
        PreTurn(true);
        cantcall = precall; // 次のコールは前回のコールと同じ数字は不可。
        precall = pprecall;
        if (pprecall != 0)
        {
            PreTurn(true);
            pv.RPC("SetBlowoff", RpcTarget.All, turn, "" + pprecall);
            NextTurn(true);
        }
        Turn();
    }

    // (ホスト専用)途中参加のプレイヤー用の情報を添えてSetupForMidwayを送る。
    [PunRPC]
    void SendMidway()
    {
        pv.RPC("SetupForMidway", RpcTarget.Others, playerNum, lives.ToArray(), randomIDs);
    }

    [PunRPC]
    void SetupForMidway(int pn, int[] lvs, int[] rids)
    {
        if (PlayerPrefs.GetInt(PhotonNetwork.NickName + "midway") == 0)
        {
            return;
        }
        PlayerPrefs.SetInt(PhotonNetwork.NickName + "midway", 0);
        playerNum = pn;
        randomIDs = rids;
        Display();
        DisplayLives();
        for (int i = 0; i < playerNum; i++)
        {
            survives.Add(true);
            if (lvs[i] < 2)
            {
                LessLife(i + 1, 2);
            }
            if (lvs[i] < 1)
            {
                LessLife(i + 1, 1);
            }
            if (lvs[i] < 0)
            {
                LessLife(i + 1, 0);
            }
        }
        // 途中参加者数分、survivesにfalseを増やす。
        for (int i = 0; i < PhotonNetwork.PlayerList.Length-playerNum; i++)
        {
            survives.Add(false);
        }
        DisplayCard();
    }

    void Display()
    {
        for (int i = 1; i < playerNum+1; i++)
        {
            int index = GetListID(i);
            int id = -1;
            for (int j = 0; j < playerNum; j++)
            {
                if (randomIDs[j] == i)
                {
                    id = j + 1;
                }
            }
            names[index].SetActive(true);
            names[index].GetComponent<TMP_Text>().text = i + "." + PhotonNetwork.PlayerList[id - 1].NickName;
        }
    }

    void DisplayLives()
    {
        for (int i = 1; i < playerNum+1; i++)
        {
            int j = GetListID(i);
            life_images[2*j].SetActive(true);
            life_images[2*j+1].SetActive(true);
        }
    }

    [PunRPC]
    void DisplayCard()
    {
        GameObject card_prefab = Resources.Load<GameObject>("Card");
        GameObject card;
        CardModel cardModel;

        for (int i = 0; i < playerNum; i++)
        {
            if (survives[i] == false)
            {
                continue;
            }
            int j = GetListID(i + 1);
            card = Instantiate(card_prefab);
            card.transform.SetParent(hands[j].transform, false);
            cardModel = card.GetComponent<CardModel>();
            cardModel.cardIndex = (int)getRoomProperty("card" + (i + 1));
            if ((i + 1) != myID)
            {
                cardModel.ToggleFace(true);
            }
            cards.Add(card);
        }
        itemUsed = false;

        // ホストにTurnをしてもらう。
        if (!PhotonNetwork.IsMasterClient && survives[myID - 1])
        {
            pv.RPC("Ready", RpcTarget.MasterClient, "Turn");
        }
    }

    // 自分のカードをオープンする。
    [PunRPC]
    void OpenMyCard()
    {
        GameObject card;
        CardModel cardModel;
        foreach (Transform childTransform in hands[0].transform)
        {
            card = childTransform.gameObject;
            cardModel = card.GetComponent<CardModel>();
            cardModel.ToggleFace(true);
        }
    }

    // ? or DRAW1による追加カードを表示させる。
    // (n: 追加するカードID, id: カードを追加するプレイヤーID, d: DRAW1によるものか)
    [PunRPC]
    void AddDisplayCard(int n, int id, bool d)
    {
        int j = GetListID(id);
        GameObject card_prefab = Resources.Load<GameObject>("Card");
        GameObject card = Instantiate(card_prefab);
        card.transform.SetParent(hands[j].transform, false);
        CardModel cardModel = card.GetComponent<CardModel>();
        cardModel.cardIndex = n;
        if (!d || id != myID)
        {
            cardModel.ToggleFace(true);
        }
        cards.Add(card);
    }

    // 1ゲーム終了後に場のカードを消去する。
    [PunRPC]
    void DestroyCards()
    {
        int l = cards.Count;
        for (int i = 0; i < l; i++)
        {
            Destroy(cards[0]);
            cards.RemoveAt(0);
        }
        if (PhotonNetwork.IsMasterClient)
        {
            cards_num.Clear();
        }
        else if (survives[myID-1])
        {
            pv.RPC("Ready", RpcTarget.MasterClient, "Deal");
        }
    }

    // n: turn, pc: precall, rs: room_same, rd: room_draw1, cc: cantcall
    [PunRPC]
    void MyTurn(int n, int pc, bool rs, bool rd, int cc)
    {
        turn = n;
        // 自分が手番である場合は2つのボタンを有効にする。
        if (myID == n)
        {
            cantcall = cc;
            precall = pc;
            room_same = rs;
            room_draw1 = rd;
            SetButtonInteractable(true, -1);
        }
        NameHighLight(n);
    }

    [PunRPC]
    void SetBlowoff(int id, string n)
    {
        int i = GetListID(id);
        blowoffs[i].SetActive(true);
        blowoffs[i].GetComponentInChildren<TMP_Text>().text = n;
    }

    [PunRPC]
    void OffBlowoff()
    {
        for (int i = 0; i < playerNum; i++)
        {
            int j = GetListID(i + 1);
            blowoffs[j].SetActive(false);
        }
    }

    // idのプレイヤーのライフイラスト表示を残機nに応じて変える。
    [PunRPC]
    void LessLife(int id, int n)
    {
        int i = GetListID(id);
        if (n > -1)
        {
            life_images[2 * i + n].SetActive(false);
        }
        if (n == -1)
        {
            survives[id - 1] = false;
            names[i].GetComponent<TMP_Text>().color = Color.gray;
            names[i].GetComponent<TMP_Text>().outlineWidth = 0;
        }
    }

    // 自分が0, その他が1以降のList用インデックスを取得する。
    int GetListID(int id)
    {
        int i;
        if (id == myID)
        {
            i = 0;
        }
        else
        {
            if (id < myID)
            {
                i = id;
            }
            else
            {
                i = id - 1;
            }
        }
        return i;
    }

    void Calculate()
    {
        total = 0;

        bool maxZero = false;
        int maxID = -1;
        if (cards_num.Contains(13))
        {
            maxZero = true;
            maxID = MaxID();
        }

        foreach (int n in cards_num)
        {
            if (maxZero && n == maxID)
            {
                maxZero = false;
                continue;
            }
            if (n < 6) // 0 ～ 5
            {
                total += n;
            }
            else if (n < 12) // 10, 15, 20, -5, -10, 0
            {
                total += exceptNums[n - 6];
            }
        }

        if (cards_num.Contains(12))
        {
            total *= 2;
        }
        //Debug.Log("total: " + total);
    }

    int MaxID()
    {
        int max = -11;
        int id = -1;
        foreach (int n in cards_num)
        {
            if (n < 6) // 0 ～ 5
            {
                if (n > max)
                {
                    max = n;
                    id = n;
                }
            }
            else if (n < 12) // 10, 15, 20, -5, -10, 0
            {
                if (exceptNums[n - 6] > max)
                {
                    max = exceptNums[n - 6];
                    id = n;
                }
            }
        }
        return id;
    }

    // turnを次の手番にする。(direct: falseはPreTurn経由)
    void NextTurn(bool direct)
    {
        if (direct && room_reverse)
        {
            PreTurn(false);
            return;
        }
        do
        {
            turn = (turn % playerNum) + 1;
        } while (survives[turn - 1] == false);
    }

    // turnを前の手番にする。(direct: falseはNextTurn経由)
    void PreTurn(bool direct)
    {
        if (!room_same && room_shot)
        {
            turn = shotPreID;
        }
        else if (direct && room_reverse)
        {
            NextTurn(false);
        }
        else
        {
            do
            {
                turn -= 1;
                if (turn < 1)
                {
                    turn += playerNum;
                }
            } while (survives[turn - 1] == false);
        }
        if (room_same)
        {
            room_same = false;
            PreTurn(true);
        }
    }

    // namesの色を変化させる。
    void NameHighLight(int id)
    {
        for (int i = 1; i < playerNum + 1; i++)
        {
            int j = GetListID(i);
            if (i == id)
            {
                names[j].GetComponent<TMP_Text>().color = Color.yellow;
                names[j].GetComponent<TMP_Text>().outlineColor = Color.black;
                names[j].GetComponent<TMP_Text>().outlineWidth = (float)0.2;
            }
            else
            {
                if (survives[i - 1])
                {
                    names[j].GetComponent<TMP_Text>().color = Color.black;
                    names[j].GetComponent<TMP_Text>().outlineWidth = 0;
                }
                else
                {
                    names[j].GetComponent<TMP_Text>().color = Color.gray;
                    names[j].GetComponent<TMP_Text>().outlineWidth = 0;
                }
            }
        }
    }

    // IDをシャッフルする。
    public void Shuffle(int[] IDs)
    {
        int n = playerNum; //整数ｎの初期値はカードの枚数とする。
        while (n > 1)　//nが１より大きい場合下記を繰り返す。
        {
            n--;　//nをディクリメント
            int k = UnityEngine.Random.Range(0, n + 1);
            int temp = IDs[k];
            IDs[k] = IDs[n];
            IDs[n] = temp;
        }
    }

    // コール, コヨーテおよびアイテムボタンの有効無効を設定する。
    // (n: -1以外の時そのインデックスのみ有効にする。)
    void SetButtonInteractable(bool b, int n)
    {
        button0.interactable = b;
        if (precall == 0)
        {
            button1.interactable = false;
        }
        else
        {
            button1.interactable = b;
        }
        if (itemUsed)
        {
            b = false;
        }
        for (int i = 0; i < 6; i++)
        {
            if (precall == 0 && (i == 0 || i== 4)) // 最初のターンはSAME, CHANGEは無効
            {
                items[i].interactable = false;
            }
            else if (n != -1 && i != n) { // nがインデックス指定の時、それ以外は無効
                items[i].interactable = false;
            }
            else
            {
                items[i].interactable = b;
            }
        }
        if (room_same)
        {
            items[0].interactable = false;
        }
        if (room_draw1)
        {
            items[3].interactable = false;
        }
    }

    public void OnCallButtonClick()
    {
        if (string.IsNullOrEmpty(callNumTF.text))
        {
            return;
        }
        int call = Int32.Parse(callNumTF.text);
        if (call > precall && call != cantcall)
        {
            SetButtonInteractable(false, -1);
            pv.RPC("SetCallNum", RpcTarget.MasterClient, call, item_double, item_reverse, item_draw1, item_shot, selectID);
            item_double = false;
            item_reverse = false;
            item_draw1 = false;
            item_shot = false;
            cantcall = 0;
        }
    }

    public void OnCoyoteButtonClick()
    {
        SetButtonInteractable(false, -1);
        pv.RPC("Coyote", RpcTarget.MasterClient);
    }

    public void OnSameButtonClick()
    {
        SetButtonInteractable(false, -1);
        itemUsed = true;
        pv.RPC("Same", RpcTarget.MasterClient);
    }

    public void OnDoubleButtonClick()
    {
        if (item_double)
        {
            item_double = false;
            itemUsed = false;
            SetButtonInteractable(true, -1);
        }
        else
        {
            SetButtonInteractable(true, 1);
            button1.interactable = false;
            item_double = true;
            itemUsed = true;
        }
    }

    public void OnReverseButtonClick()
    {
        if (item_reverse)
        {
            item_reverse = false;
            itemUsed = false;
            SetButtonInteractable(true, -1);
        }
        else
        {
            SetButtonInteractable(true, 2);
            button1.interactable = false;
            item_reverse = true;
            itemUsed = true;
        }
    }

    public void OnDraw1ButtonClick()
    {
        if (item_draw1)
        {
            item_draw1 = false;
            itemUsed = false;
            selectName = false;
            SetButtonInteractable(true, -1);
            NameHighLight(turn);
        }
        else
        {
            SetButtonInteractable(true, 3);
            button0.interactable = false; // プレイヤー選択までコール無効
            button1.interactable = false;
            item_draw1 = true;
            itemUsed = true;
            selectName = true;
        }
    }

    public void OnChangeButtonClick()
    {
        SetButtonInteractable(false, -1);
        itemUsed = true;
        pv.RPC("Change", RpcTarget.MasterClient);
    }

    public void OnShotButtonClick()
    {
        if (item_shot)
        {
            item_shot = false;
            itemUsed = false;
            selectName = false;
            SetButtonInteractable(true, -1);
            NameHighLight(turn);
        }
        else
        {
            SetButtonInteractable(true, 5);
            button0.interactable = false; // プレイヤー選択までコール無効
            button1.interactable = false;
            item_shot = true;
            itemUsed = true;
            selectName = true;
        }
    }

    public void OnNextButtonClick()
    {
        pv.RPC("OffBlowoff", RpcTarget.All);
        pv.RPC("DestroyCards", RpcTarget.All);
        nextButton.SetActive(false);
    }

    public void OnBackButtonClick()
    {
        AddRoomProperty("start", 0); // 0: ゲーム開始前
        SceneManager.LoadScene("Robby");
        pv.RPC("SceneTranToRobby", RpcTarget.Others);
    }

    public void OnNameClick(int n)
    {
        if (!selectName)
        {
            return;
        }
        if (n == 0)
        {
            selectID = myID;
        }
        else if (n < myID)
        {
            if (!survives[n - 1])
            {
                return;
            }
            selectID = n;
        }
        else
        {
            if (!survives[n])
            {
                return;
            }
            selectID = n + 1;
        }
        NameHighLight(turn);
        names[n].GetComponent<TMP_Text>().color = Color.red;
        names[n].GetComponent<TMP_Text>().outlineColor = Color.black;
        names[n].GetComponent<TMP_Text>().outlineWidth = (float)0.2;
        button0.interactable = true;
    }

    [PunRPC]
    void SceneTranToRobby()
    {
        SceneManager.LoadScene("Robby");
    }

    [PunRPC]
    void Ready(string nextMethod)
    {
        readyPlayerNum++;
        if ((survives[myID - 1] && arriveNum == readyPlayerNum + 1) ||
            (!survives[myID - 1] && arriveNum == readyPlayerNum))
        {
            pv.RPC(nextMethod, RpcTarget.MasterClient);
            readyPlayerNum = 0;
        }
    }

    [PunRPC]
    void ReadyAll(string nextMethod)
    {
        readyPlayerNum++;
        if ((survives[myID - 1] && arriveNum == readyPlayerNum + 1) ||
            (!survives[myID - 1] && arriveNum == readyPlayerNum))
        {
            pv.RPC(nextMethod, RpcTarget.All);
            readyPlayerNum = 0;
        }
    }

    // ルームプロパティにキーkey, 値valueのプロパティを追加する。
    void AddRoomProperty(string key, object value)
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        object x = null;
        if (properties.TryGetValue(key, out x))
        {
            properties[key] = value;
        }
        else
        {
            properties.Add(key, value);
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    // ルームプロパティからキーkeyの値を返す。
    object getRoomProperty(string key)
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        object x = null;
        properties.TryGetValue(key, out x);
        return x;
    }
}
