using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using TMPro;

namespace KnockOff.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    //[RequireComponent(typeof(PlayerAnimatorManager))]
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region Public Fields

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        public GameObject projectile;

        #endregion

        #region Private Fields

        [SerializeField] private Item[] items;
        [SerializeField] private TextMeshPro userNameTxt; 

        private PlayerMovement playerMovement;
        private int itemIndex;
        private int previousItem = -1;

        #endregion

        public PhotonTeam playerTeam { get; private set; }
        public string playerUsername { get; private set; }
        public bool IsFiring { get; set; }      //networked

        #region Monobehaviour Callbacks

        private void Awake()
        {
            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                PlayerManager.LocalPlayerInstance = this.gameObject;
            }
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);

            if (!TryGetComponent(out playerMovement))
                Debug.LogError("<Color=Red><a>Missing</a></Color> Player Movement Component on playerPrefab.", this);
            /*
            if (!TryGetComponent(out playerAnimatorManager))
                Debug.LogError("<Color=Red><a>Missing</a></Color> Player Animation Manager Component on playerPrefab.", this);*/

            PhotonTeamsManager.PlayerJoinedTeam += AlertPlayersAboutTeams;
        }

        private void OnDestroy()
        {
            PhotonTeamsManager.PlayerJoinedTeam -= AlertPlayersAboutTeams;
        }


        private void Start()
        {
            EquipItem(0);
        }

        private void Update()
        {
            //GameManager.Instance.LeaveRoom() -> losing condition

            if (photonView.IsMine)
            {
                ProcessInputs();
                WeaponSwitchInputs();
            }
        }

        private void AlertPlayersAboutTeams(Photon.Realtime.Player p, PhotonTeam team)
        {
            if (photonView.IsMine)
            {
                playerTeam = team;
                playerUsername = p.NickName;
                Debug.LogFormat("{0}, You have been assigned to the <Color={1}><a>{1}</a></Color> team.", p.NickName, playerTeam.Name);

                // Set the TagObject property to the player's GameObject
                p.TagObject = this.gameObject;

                //only show username to other players, not myself
                photonView.RPC("SetPlayerNameForOtherPlayers", RpcTarget.Others, playerUsername, playerTeam.Name);
            }
        }

        private void WeaponSwitchInputs()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    photonView.RPC("EquipItem",RpcTarget.All,i);
                    break;
                }
            }
            #region Remove when testing is done This is for scrollwheel
            //if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            //{
            //    if (itemIndex >= items.Length - 1)
            //    {
            //        photonView.RPC("EquipItem", RpcTarget.All, 0);
            //    }
            //    else
            //    {
            //        photonView.RPC("EquipItem", RpcTarget.All, itemIndex + 1);
            //    }
            //}
            //else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            //{
            //    if (itemIndex <= 0)
            //    {
            //        photonView.RPC("EquipItem", RpcTarget.All, items.Length - 1);
            //    }
            //    else
            //    {
            //        photonView.RPC("EquipItem", RpcTarget.All, itemIndex - 1);
            //    }
            //}
            #endregion
        }

        /// <summary>
        /// MonoBehaviour method called when the Collider 'other' enters the trigger.
        /// Knock off player if the collider is from opposite team
        /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
        /// One could move the collider further away to prevent this or check if the beam belongs to the player.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine)    // we dont' do anything if we are not the local player.
                return;

        }

        private void OnTriggerStay(Collider other)
        {
            if (!photonView.IsMine)    // we dont' do anything if we are not the local player.
                return;

        }

        #endregion


        #region IPunObservable implementation
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {    // We own this player: send the others our data
                stream.SendNext(IsFiring);
                stream.SendNext(playerMovement.isGrounded);
                stream.SendNext(playerMovement.isSprinting);
            }
            else
            {    // Network player, receive data
                this.IsFiring = (bool)stream.ReceiveNext();
                this.playerMovement.isGrounded = (bool)stream.ReceiveNext();
                this.playerMovement.isSprinting = (bool)stream.ReceiveNext();
            }
        }

        #endregion

        public void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (!IsFiring)
                {
                    items[itemIndex].Use();
                    IsFiring = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
            }
        }
        [PunRPC]
        void EquipItem(int  index)
        {
            if(index == previousItem)
            {
                return;
            }
            itemIndex= index;

            items[itemIndex].itemPrefab.SetActive(true);

            if (previousItem != -1)
            {
                items[previousItem].itemPrefab.SetActive(false);
            }
            previousItem = itemIndex;
        }

        [PunRPC]
        void SetPlayerNameForOtherPlayers(string playerName, string playerTeamName)
        {
            userNameTxt.text = playerName;

            string colorString = playerTeamName;
            Color color;

            if (ColorUtility.TryParseHtmlString(GetColorString(colorString), out color))
                userNameTxt.color = color;
        }

        // Helper function to get the color string for known color names
        private string GetColorString(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "red":
                    return "#FF0000";
                case "green":
                    return "#00FF00";
                case "blue":
                    return "#0000FF";
                // Add more cases for other colors as needed
                default:
                    return "#000000";
            }
        }
    }
}
