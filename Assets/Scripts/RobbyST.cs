using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class RobbyST : MonoBehaviourPunCallbacks
{
    int myID; // 部屋における自分のID(1～6)
    public Canvas canvas;
    List<GameObject> pnames;
    public Button startButton;
    PhotonView pv;

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        if (PlayerPrefs.HasKey(PhotonNetwork.NickName + "ID")) {
            myID = PlayerPrefs.GetInt(PhotonNetwork.NickName + "ID");
        }
        else
        {
            myID = PhotonNetwork.PlayerList.Length;
        }
        object x = GetRoomProperty("start");
        if (x != null && (int)x == 1)
        {
            SceneTranToGame(1);
            return;
        }
        pnames = new List<GameObject>();
        pv = GetComponent<PhotonView>();
        Display();
        if (PhotonNetwork.IsMasterClient)
        {
            AddRoomProperty("start", 0); // 0: false
            if (PhotonNetwork.PlayerList.Length > 1)
            {
                startButton.interactable = true;
            }
        }
    }

    // 他プレイヤーがルーム参加時
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Destroy();
        Display();
        // マスターはプレイヤー人数が2名以上の時は開始ボタンを有効にする。
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length > 1)
        {
            startButton.interactable = true;
        }
    }

    // 他プレイヤーがルーム退出時
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Destroy();
        Display();
        // マスターはプレイヤー人数が1名の時は開始ボタンを無効にする。
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.PlayerList.Length > 1)
        {
            startButton.interactable = true;
        }
        else if (PhotonNetwork.PlayerList.Length == 1)
        {
            startButton.interactable = false;
        }
    }

    public void OnButtonClick()
    {
        if (PhotonNetwork.PlayerList.Length != 0)
        {
            pv.RPC("ResetID", RpcTarget.Others, myID);
        }
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Title");
    }

    public void OnStartButtonClick()
    {
        AddRoomProperty("Num", PhotonNetwork.PlayerList.Length);
        PlayerPrefs.SetInt(PhotonNetwork.NickName + "ID", myID);
        PlayerPrefs.SetInt(PhotonNetwork.NickName + "midway", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
        pv.RPC("SceneTranToGame", RpcTarget.Others, 0);
    }

    [PunRPC]
    void ResetID(int x)
    {
        // 退出者よりmyIDが後ろのプレイヤーは前に詰める。
        if (myID > x)
        {
            myID -= 1;
        }
    }

    void Destroy()
    {
        while (pnames.Count != 0)
        {
            Destroy(pnames[0]);
            pnames.RemoveAt(0);
        }
    }

    void Display()
    {
        Vector3 v;
        GameObject pname_prefab = Resources.Load<GameObject>("PlayerName");
        GameObject pname = Instantiate(pname_prefab);

        // 自身の名前表示
        v = new Vector3(-305, -139, 0);
        pname.transform.position = v;
        pname.GetComponent<TMP_Text>().text = PhotonNetwork.NickName;
        pname.transform.SetParent(canvas.transform, false);
        pnames.Add(pname);

        int i = 1;
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerListOthers)
        {
            pname_prefab = Resources.Load<GameObject>("PlayerName");
            pname = Instantiate(pname_prefab);
            v = new Vector3(-305 + 400 * ((i - 1) / 3), 171 - 104 * ((i - 1) % 3), 0);
            pname.transform.position = v;
            pname.GetComponent<TMP_Text>().text = p.NickName;
            pname.transform.SetParent(canvas.transform, false);
            pnames.Add(pname);
            i++;
        }
    }

    [PunRPC]
    void SceneTranToGame(int midway) // midway: 途中参加(0: false, 1: true)
    {
        PlayerPrefs.SetInt(PhotonNetwork.NickName + "ID", myID);
        PlayerPrefs.SetInt(PhotonNetwork.NickName + "midway", midway);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
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
    object GetRoomProperty(string key)
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        object x = null;
        properties.TryGetValue(key, out x);
        return x;
    }
}
