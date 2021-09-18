using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class PreTitleST : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.DeleteAll();
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        SceneManager.LoadSceneAsync("Title");
    }
}
