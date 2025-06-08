using Unity.Netcode;
using UnityEngine;

public abstract class Item : NetworkBehaviour
{
    public ItemInfo ItemInfo;
    public GameObject ItemGameObject;

    public abstract void Use();
}
