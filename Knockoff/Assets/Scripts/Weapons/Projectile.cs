using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KnockOff.Player;

public class Projectile : MonoBehaviourPunCallbacks
{
    [SerializeField] float expForce = 100f;
    [SerializeField] float radius = 2f;
    [SerializeField] Transform HitAudio;

    private PlayerMovement playerMovement;
    public Photon.Realtime.Player playerOwner { get; set; }

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerManager>() != null && collision.gameObject.GetComponentInParent<PlayerManager>().localPlayer)
        {
            Debug.Log("this is local");
            return;
        }
        else if (collision.transform.tag == "Player")
        {
            int targetPlayerID = collision.gameObject.GetComponentInParent<PhotonView>().ViewID;
            Vector3 contactPoint = collision.contacts[0].point;
            photonView.RPC("KnockBackPlayer", RpcTarget.Others, targetPlayerID, playerOwner, expForce, radius, contactPoint); ;
            Instantiate(HitAudio, transform.position, Quaternion.identity);
        }
    }

    [PunRPC]
    private void KnockBackPlayer(int targetPlayerID, Photon.Realtime.Player attackingPlayer , float expForce, float radius, Vector3 contactPoint)
    {
        PhotonView pv = PhotonView.Find(targetPlayerID);

        if (pv.IsMine)
        {
            Rigidbody exPlode = pv.GetComponent<Rigidbody>();
            pv.GetComponent<PlayerRespawn>().Opponent = attackingPlayer;
            Vector3 knockbackDir = (photonView.transform.position - contactPoint).normalized;
            exPlode.AddForceAtPosition(-knockbackDir * expForce, contactPoint, ForceMode.Impulse);
            playerMovement.anim.SetBool("GotHit", true);
        }
    }
}
