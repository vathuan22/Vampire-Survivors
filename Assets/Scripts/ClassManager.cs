using System.Collections.Generic;
using UnityEngine;

public class ClassManager : MonoBehaviour
{
    public static ClassManager instance;

    [Header("Available Classes (all possible classes in the game)")]
    public List<ClassData> allClasses = new List<ClassData>();

    [Header("Runtime State")]
    public List<ClassData> playerClasses = new List<ClassData>();
    public int activeClassIndex = 0;
    public int maxClasses = 3;
    
    [HideInInspector]
    public ClassData firstClassSelected;  // ✅ Lưu class đầu tiên được chọn

    private void Awake()
    {
        instance = this;
        Debug.Log($"🔍 ClassManager.Awake - playerClasses.Count: {playerClasses.Count}");
        if (playerClasses.Count > 0)
        {
            for (int i = 0; i < playerClasses.Count; i++)
            {
                Debug.Log($"  Class {i}: {playerClasses[i]?.className ?? "NULL"}");
            }
        }
    }

    public ClassData ActiveClass
    {
        get
        {
            if (playerClasses.Count == 0) return null;
            if (activeClassIndex < 0 || activeClassIndex >= playerClasses.Count) return null;
            return playerClasses[activeClassIndex];
        }
    }

    public bool HasNoClass => playerClasses.Count == 0;
    public bool CanUnlockNewClass => playerClasses.Count < maxClasses;

    public bool HasClass(ClassData classData)
    {
        return classData != null && playerClasses.Contains(classData);
    }

    public void SetActiveClass(int index)
    {
        if (index < 0 || index >= playerClasses.Count) return;
        activeClassIndex = index;

        if (PlayerController.instance != null)
            PlayerController.instance.ApplyClass(ActiveClass);
    }

    public void UnlockClass(ClassData classData)
    {
        if (classData == null) return;
        if (playerClasses.Contains(classData)) return;
        if (playerClasses.Count >= maxClasses) return;

        // ✅ Track class đầu tiên được chọn
        if (playerClasses.Count == 0)
            firstClassSelected = classData;

        playerClasses.Add(classData);

        if (playerClasses.Count == 1)
            activeClassIndex = 0;
    }

    public void ApplyClass(ClassData classData)
    {
        if (classData == null) return;

        if (!playerClasses.Contains(classData))
        {
            if (playerClasses.Count >= maxClasses)
                return;

            playerClasses.Add(classData);
        }

        activeClassIndex = playerClasses.IndexOf(classData);

        if (PlayerController.instance != null)
            PlayerController.instance.ApplyClass(classData);
    }

    public void PromoteClass(ClassData classData)
    {
        if (classData == null) return;
        if (classData.promotionClass == null) return;

        int idx = playerClasses.IndexOf(classData);
        if (idx < 0) return;

        // ✅ CLEAR TẤT CẢ weapons trước promote (stop all coroutines, effects)
        if (PlayerController.instance != null)
        {
            // Disable tất cả weapons và stop coroutines
            Weapon[] allWeapons = PlayerController.instance.GetComponentsInChildren<Weapon>(true);
            foreach (Weapon weapon in allWeapons)
            {
                if (weapon != null)
                {
                    weapon.StopActiveSkill();
                    weapon.gameObject.SetActive(false);
                }
            }
            Debug.Log($"✅ Disabled all {allWeapons.Length} weapons before promote");
        }

        // ✅ Update firstClassSelected → promotion class (để sprite thay đổi)
        if (classData == firstClassSelected)
        {
            firstClassSelected = classData.promotionClass;
            Debug.Log($"✅ Promoted first class: {classData.className} → {classData.promotionClass.className}");
        }

        // ✅ Replace class trong list (giữ overlays, không remove)
        playerClasses[idx] = classData.promotionClass;
        activeClassIndex = idx;

        if (PlayerController.instance != null)
        {
            PlayerController.instance.ApplyClass(classData.promotionClass);
            // ✅ Overlays vẫn ở, không cần reapply vì không remove
        }
    }

    public List<ClassData> GetUnlockableClasses()
    {
        var result = new List<ClassData>();

        foreach (var c in allClasses)
        {
            if (c == null) continue;
            if (playerClasses.Contains(c)) continue;  // ✅ Đã có rồi
            
            // ✅ Bỏ qua class cũ (nếu promotion của nó đã được nâng)
            // Ví dụ: Nếu ArchMage đã được nâng, bỏ qua Mage
            if (c.promotionClass != null && playerClasses.Contains(c.promotionClass))
            {
                continue;  // Skip - class này là old version
            }
            
            result.Add(c);
        }

        return result;
    }

    public List<Weapon> GetAvailableWeaponsForClass(ClassData classData)
    {
        var available = new List<Weapon>();
        if (classData == null) return available;
        if (PlayerController.instance == null) return available;
        if (classData.classWeapons == null) return available;

        foreach (var weaponPrefab in classData.classWeapons)
        {
            if (weaponPrefab == null) continue;

            // Tìm instance weapon này từ prefab
            Weapon foundWeapon = null;

            foreach (var w in PlayerController.instance.assignedWeapons)
            {
                if (w != null && w.weaponPrefab == weaponPrefab)
                {
                    foundWeapon = w;
                    break;
                }
            }

            if (foundWeapon == null)
            {
                foreach (var w in PlayerController.instance.unassignedWeapons)
                {
                    if (w != null && w.weaponPrefab == weaponPrefab)
                    {
                        foundWeapon = w;
                        break;
                    }
                }
            }

            // Nếu tìm được và chưa max, thêm vào available
            if (foundWeapon != null)
            {
                bool isMaxed = PlayerController.instance.fullyLevelledWeapons.Contains(foundWeapon);
                if (!isMaxed)
                    available.Add(foundWeapon);
            }
        }

        return available;
    }

    public bool AreAllWeaponsMaxedForClass(ClassData classData)
    {
        if (classData == null) return false;
        if (PlayerController.instance == null) return false;
        if (classData.classWeapons == null || classData.classWeapons.Count == 0) return false;

        foreach (var weaponPrefab in classData.classWeapons)
        {
            if (weaponPrefab == null) continue;

            // Tìm instance weapon từ prefab này
            Weapon foundWeapon = null;

            foreach (var w in PlayerController.instance.assignedWeapons)
            {
                if (w != null && w.weaponPrefab == weaponPrefab)
                {
                    foundWeapon = w;
                    break;
                }
            }

            if (foundWeapon == null)
            {
                foreach (var w in PlayerController.instance.fullyLevelledWeapons)
                {
                    if (w != null && w.weaponPrefab == weaponPrefab)
                    {
                        foundWeapon = w;
                        break;
                    }
                }
            }

            // Nếu không tìm được hoặc chưa max level → false
            if (foundWeapon == null || !PlayerController.instance.fullyLevelledWeapons.Contains(foundWeapon))
                return false;
        }

        return true;
    }
    }
