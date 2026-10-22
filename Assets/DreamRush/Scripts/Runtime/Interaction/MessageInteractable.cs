using System.Collections;
using DreamRush.Flow;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Interaction
{
    /// <summary>Flavour text objects: window, door, posters (environmental storytelling, GDD §7.8).</summary>
    public class MessageInteractable : Interactable
    {
        public string title = "";
        [TextArea] public string body = "";
        protected override void OnInteract(InteractionContext context) => MessagePanel.Show(title, body);
    }
}
