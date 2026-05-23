using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    public float moveSpeed;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public float pickupRange = 1.5f;

    public List<Weapon> unassignedWeapons = new List<Weapon>();
    public List<Weapon> assignedWeapons = new List<Weapon>();
    public int maxWeapons = 3;

    [HideInInspector]
    public List<Weapon> fullyLevelledWeapons = new List<Weapon>();

    [HideInInspector]
    private List<GameObject> activeOverlays = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
{
    CacheComponents();

    if (PlayerStatController.instance != null)
    {
        moveSpeed = PlayerStatController.instance.moveSpeed[0].value;
        pickupRange = PlayerStatController.instance.pickupRange[0].value;
        maxWeapons = Mathf.RoundToInt(PlayerStatController.instance.maxWeapons[0].value);
    }

    // Spawn vũ khí ban đầu cho lớp được chọn
    if (ClassManager.instance != null && ClassManager.instance.ActiveClass != null)
    {
        Debug.Log($"✅ ApplyClass: {ClassManager.instance.ActiveClass.className}");
        ApplyClass(ClassManager.instance.ActiveClass);
    }
    else
    {
        Debug.Log("❌ No active class at start");
    }

    if (UIController.instance != null)
        UIController.instance.UpdateActiveClassDisplay();
}
public void ApplyClassVisualOnly(ClassData classData)
{
    if (classData == null) return;

    CacheComponents();

    if (classData.animatorController != null && anim != null)
        anim.runtimeAnimatorController = classData.animatorController;

    ApplyCharacterVisualFromPrefab(classData.characterPrefab);
}

    private void CacheComponents()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Thêm overlay (phụ kiện class) lên nhân vật
    /// </summary>
    public void AddOverlay(GameObject overlayPrefab)
    {
        if (overlayPrefab == null) return;

        GameObject overlay = Instantiate(overlayPrefab, transform);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        activeOverlays.Add(overlay);

        Debug.Log($"✅ Added overlay: {overlayPrefab.name}");
    }

    /// <summary>
    /// Xóa tất cả overlays
    /// </summary>
    public void RemoveAllOverlays()
    {
        foreach (var overlay in activeOverlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }
        activeOverlays.Clear();

        Debug.Log("✅ Removed all overlays");
    }

    private void Update()
    {
        if (instance == null) return;

        CacheComponents();

        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);
        moveInput.Normalize();

        transform.position += moveInput * moveSpeed * Time.deltaTime;

        if (spriteRenderer != null)
        {
            if (moveInput.x > 0) spriteRenderer.flipX = false;
            else if (moveInput.x < 0) spriteRenderer.flipX = true;
        }

        if (anim != null)
            anim.SetBool("isMoving", moveInput != Vector3.zero);
    }

    public void ApplyClass(ClassData classData)
{
    if (classData == null) return;

    CacheComponents();

    

    // ✅ Chỉ thay đổi visual nếu đây là class đầu tiên hoặc promotion
    if (ClassManager.instance != null && classData == ClassManager.instance.firstClassSelected)
    {
        
        
        if (classData.animatorController != null && anim != null)
        {
            
            anim.runtimeAnimatorController = classData.animatorController;
        }
        else
        {
            Debug.LogWarning($"⚠️ Animator or animatorController is null!");
        }

        ApplyCharacterVisualFromPrefab(classData.characterPrefab);
    }
    else
    {
        Debug.LogWarning($"❌ NOT applying visual - firstClassSelected mismatch or null ClassManager");
    }

    // ✅ Nếu là secondary class (không phải first), chỉ thêm overlay + weapons
    // ✅ Secondary = classData khác với active class hiện tại (giữ weapons của primary)
    bool isSecondaryClass = ClassManager.instance != null && 
        ClassManager.instance.ActiveClass != null && 
        classData != ClassManager.instance.ActiveClass;

    // ✅ CHỈ clear weapons nếu là primary class, secondary class giữ weapons của primary
    if (!isSecondaryClass)
    {
        DisableCurrentWeapons();
        assignedWeapons.Clear();
        unassignedWeapons.Clear();  
        fullyLevelledWeapons.Clear();
    }

    // ✅ Chỉ spawn starter weapon khi chuyển class (primary class)
    if (!isSecondaryClass && classData.starterWeapon != null)
        SpawnAndAssignWeapon(classData.starterWeapon);

    // ✅ Các weapons khác trong classWeapons được thêm vào unassignedWeapons (chưa active)
    if (classData.classWeapons != null)
    {
        foreach (GameObject weaponPrefab in classData.classWeapons)
        {
            if (weaponPrefab == null) continue;
            if (!isSecondaryClass && classData.starterWeapon != null && weaponPrefab == classData.starterWeapon) continue;

            // Tạo weapon nhưng KHÔNG activate nó, để vào unassignedWeapons
            GameObject weaponObj = Instantiate(weaponPrefab, transform);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.SetActive(false);  // ← K kích hoạt!

            Weapon weapon = weaponObj.GetComponent<Weapon>();
            if (weapon != null)
            {
                weapon.weaponPrefab = weaponPrefab;
                unassignedWeapons.Add(weapon);  // ← Thêm vào unassignedWeapons
                Debug.Log($"✅ Created weapon: {weapon.name} (unassigned, {(isSecondaryClass ? "secondary class" : "waiting for level-up choice")})");
            }
        }
    }

    // ✅ Thêm overlay nếu là secondary class
    if (isSecondaryClass && classData.overlayPrefab != null)
    {
        AddOverlay(classData.overlayPrefab);
    }

    if (UIController.instance != null)
        UIController.instance.UpdateActiveClassDisplay();
}
    private void DisableCurrentWeapons()
{
   Weapon[] weapons = GetComponentsInChildren<Weapon>(true);
   foreach (Weapon weapon in weapons)
   {
       if (weapon != null)
          weapon.gameObject.SetActive(false);
  }
}
private void SpawnAndAssignWeapon(GameObject weaponPrefab)
{
    if (weaponPrefab == null) return;

    GameObject weaponObj = Instantiate(weaponPrefab, transform);
    weaponObj.transform.localPosition = Vector3.zero;
    weaponObj.transform.localRotation = Quaternion.identity;
    weaponObj.SetActive(true);

    Weapon weapon = weaponObj.GetComponent<Weapon>();
    if (weapon != null)
    {
        weapon.weaponPrefab = weaponPrefab;  // ✅ Track prefab
        
        // ✅ Check nếu weapon đã max level → add vào fullyLevelledWeapons
        if (weapon.IsMaxLevel())
        {
            fullyLevelledWeapons.Add(weapon);
            Debug.Log($"✅ Spawned weapon (MAX LEVEL): {weapon.name}");
        }
        else
        {
            assignedWeapons.Add(weapon);
            Debug.Log($"✅ Spawned weapon (Level {weapon.weaponLevel}): {weapon.name}");
        }
    }
    else
    {
        Debug.LogWarning($"⚠️ {weaponObj.name} has no Weapon component!");
    }
}
    private void ClearCurrentWeapons()
{
    for (int i = assignedWeapons.Count - 1; i >= 0; i--)
    {
        if (assignedWeapons[i] != null)
            assignedWeapons[i].gameObject.SetActive(false);
    }

    assignedWeapons.Clear();
    unassignedWeapons.Clear();
    fullyLevelledWeapons.Clear();
}
    private void ApplyCharacterVisualFromPrefab(GameObject characterPrefab)
    {
        if (characterPrefab == null) 
        {
            Debug.LogWarning("⚠️ characterPrefab is NULL!");
            return;
        }

        

        Animator prefabAnimator = characterPrefab.GetComponentInChildren<Animator>();
        SpriteRenderer prefabSprite = characterPrefab.GetComponentInChildren<SpriteRenderer>();

        

        if (prefabAnimator != null && anim != null)
        {
            
            anim.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
        }

        if (prefabSprite != null && spriteRenderer != null)
        {
            Debug.Log($"✅ Setting sprite: {prefabSprite.sprite?.name ?? "NULL"}");
            spriteRenderer.sprite = prefabSprite.sprite;
            spriteRenderer.color = prefabSprite.color;
            spriteRenderer.sortingLayerID = prefabSprite.sortingLayerID;
            spriteRenderer.sortingOrder = prefabSprite.sortingOrder;
        }
        else
        {
            Debug.LogWarning($"❌ prefabSprite or spriteRenderer is NULL!");
        }
    }

    public bool HasWeapon(Weapon weapon)
    {
        return weapon != null &&
               (assignedWeapons.Contains(weapon) || fullyLevelledWeapons.Contains(weapon));
    }

    public void AddWeapon(int weaponNumber)
    {
        if (weaponNumber < 0 || weaponNumber >= unassignedWeapons.Count) return;

        Weapon weapon = unassignedWeapons[weaponNumber];
        if (weapon == null) return;
        if (HasWeapon(weapon)) return;

        assignedWeapons.Add(weapon);
        weapon.gameObject.SetActive(true);
        unassignedWeapons.RemoveAt(weaponNumber);
    }

    public void AddWeapon(Weapon weaponToAdd)
    {
        if (weaponToAdd == null) return;
        if (HasWeapon(weaponToAdd)) return;

        weaponToAdd.gameObject.SetActive(true);
        
        // Check if weapon is already MAX level
        if (weaponToAdd.IsMaxLevel())
        {
            fullyLevelledWeapons.Add(weaponToAdd);
            Debug.Log($"✅ Added {weaponToAdd.name} (MAX LEVEL) to fullyLevelledWeapons");
        }
        else
        {
            assignedWeapons.Add(weaponToAdd);
            Debug.Log($"✅ Added {weaponToAdd.name} (Level {weaponToAdd.weaponLevel}) to assignedWeapons");
        }
        
        unassignedWeapons.Remove(weaponToAdd);
    }

    public void GiveStarterWeapon(GameObject starterWeaponObj)
    {
        if (starterWeaponObj == null) return;

        Weapon starterWeapon = starterWeaponObj.GetComponent<Weapon>();
        if (starterWeapon == null) return;

        GiveStarterWeapon(starterWeapon);
    }

    public void GiveStarterWeapon(Weapon starterWeapon)
    {
        if (starterWeapon == null) return;
        if (!HasWeapon(starterWeapon))
            AddWeapon(starterWeapon);
    }
}