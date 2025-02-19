// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH"/>
// <summary>Demo code for Photon Chat in Unity.</summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace Photon.Chat.Demo
{
    public class ChannelSelector : MonoBehaviour, IPointerClickHandler
    {
        public string channel;

        public void SetChannel(string channel)
        {
            this.channel = channel;
            Text t = this.GetComponentInChildren<Text>();
            t.text = this.channel;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ChatGui handler = FindAnyObjectByType<ChatGui>();
            handler.ShowChannel(this.channel);
        }
    }
}