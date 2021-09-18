using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class TitleST : MonoBehaviourPunCallbacks
{
    public TMP_InputField nameTF, roomTF;

    public void OnButtonClick()
    {
        string name = nameTF.text.Replace(" ", "").Replace("　", "");
        string room = roomTF.text.Replace(" ", "").Replace("　", "");
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(room))
        {
            PhotonNetwork.NickName = nameTF.text;
            PhotonNetwork.JoinOrCreateRoom(roomTF.text, new RoomOptions() { MaxPlayers = 7 }, TypedLobby.Default);
        }
    }

    // ルーム作成時
    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        //Debug.Log("ルームを作成しました。");
    }

    // ルーム参加時
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        //Debug.Log("ルームを入室しました。");
        PhotonNetwork.IsMessageQueueRunning = false;
        SceneManager.LoadSceneAsync("Robby");
    }
}
