using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionContainer : MonoBehaviour, IInteractable
{
    [SerializeField] private IInteractable[] Interactions;
    public void Interact() {
        for(int i = 0; i < Interactions.Length; i++)
            Interactions[i].Interact();
    }
}
