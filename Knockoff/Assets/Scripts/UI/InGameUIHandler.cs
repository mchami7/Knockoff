using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class InGameUIHandler : MonoBehaviourPunCallbacks
{
    [Header("Score Properties")]
    [SerializeField] private TextMeshProUGUI team1ScoreTxt;
    [SerializeField] private TextMeshProUGUI team2ScoreTxt;

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Update team scores displayed in the game UI
        team1ScoreTxt.text = ScoreManager.instance.team1Score.ToString();
        team2ScoreTxt.text = ScoreManager.instance.team2Score.ToString();
    }

}