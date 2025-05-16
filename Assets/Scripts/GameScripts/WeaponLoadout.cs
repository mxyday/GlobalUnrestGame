using UnityEngine;

[CreateAssetMenu(fileName = "New Loadout", menuName = "Loadout")]
public class WeaponLoadout : ScriptableObject
{
    public string className;
    public string[] classItems;
}