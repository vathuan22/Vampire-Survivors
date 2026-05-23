using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Class", menuName = "Class Survival/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Class Info")]
    public string className;
    [TextArea(2, 4)]
    public string classDescription;
    public Sprite classIcon;
    [Header("Character Visual")]
    public RuntimeAnimatorController animatorController;

    [Header("Character Prefab")]
    public GameObject characterPrefab;

    [Header("Overlay Accent")]
    public GameObject overlayPrefab;

    [Header("Weapons")]
    public GameObject starterWeapon;
    public List<GameObject> classWeapons;

    [Header("Promotion")]
    public ClassData promotionClass;
    public string promotionDescription;

    [Header("Unlock")]
    [Range(0f, 1f)]
    public float secondClassUnlockChance = 0.15f;
}