using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpSellectionButton : MonoBehaviour
{
    public TMP_Text upgradeDescText, nameLevelText;
    public Image weaponIcon;

    private LevelUpChoice currentChoice;

    public void SetChoice(LevelUpChoice choice)
    {
        currentChoice = choice;

        if (choice == null)
        {
            ClearDisplay();
            return;
        }

        switch (choice.type)
        {
            case ChoiceType.Weapon:
                DisplayWeaponChoice(choice.weapon);
                break;

            case ChoiceType.SelectClass:
                DisplayClassChoice(choice.classData);
                break;

            case ChoiceType.Promotion:
                DisplayPromotionChoice(choice.classData);
                break;
        }
    }

    private void ClearDisplay()
    {
        if (upgradeDescText != null) upgradeDescText.text = "";
        if (nameLevelText != null) nameLevelText.text = "";
        if (weaponIcon != null) weaponIcon.sprite = null;
    }

    private void DisplayWeaponChoice(Weapon weapon)
    {
        if (weapon == null)
        {
            ClearDisplay();
            return;
        }

        bool alreadyOwned = PlayerController.instance != null &&
                            (PlayerController.instance.assignedWeapons.Contains(weapon) ||
                             PlayerController.instance.fullyLevelledWeapons.Contains(weapon));

        // ✅ Xoá "(Clone)" khỏi weapon name
        string weaponName = weapon.name.Replace("(Clone)", "").Trim();

        if (alreadyOwned && weapon.stats != null && weapon.stats.Count > 0)
        {
            int nextLevel = Mathf.Min(weapon.weaponLevel + 1, weapon.stats.Count - 1);
            upgradeDescText.text = weapon.stats[nextLevel].upgradeText ?? "Upgrade weapon";
            nameLevelText.text = weaponName + " - Lvl " + weapon.weaponLevel;
        }
        else if (weapon.stats != null && weapon.stats.Count > 0)
        {
            upgradeDescText.text = "Unlock " + weaponName;
            nameLevelText.text = weaponName;
        }
        else
        {
            upgradeDescText.text = "No stats configured";
            nameLevelText.text = weaponName ?? "Unknown Weapon";
        }

        if (weaponIcon != null) weaponIcon.sprite = weapon.icon;
    }

    private void DisplayClassChoice(ClassData classData)
    {
        if (classData == null)
        {
            ClearDisplay();
            return;
        }

        if (nameLevelText != null)
            nameLevelText.text = "Class: " + classData.className;

        if (upgradeDescText != null)
            upgradeDescText.text = string.IsNullOrEmpty(classData.classDescription)
                ? "Unlock the " + classData.className + " class."
                : classData.classDescription;

        if (weaponIcon != null && classData.classIcon != null)
            weaponIcon.sprite = classData.classIcon;
    }

    private void DisplayPromotionChoice(ClassData classData)
    {
        if (classData == null)
        {
            ClearDisplay();
            return;
        }

        string promoteTo = classData.promotionClass != null ? classData.promotionClass.className : "???";

        if (nameLevelText != null)
            nameLevelText.text = "Promote: " + classData.className + " → " + promoteTo;

        if (upgradeDescText != null)
            upgradeDescText.text = string.IsNullOrEmpty(classData.promotionDescription)
                ? "Promote " + classData.className + " to a stronger form!"
                : classData.promotionDescription;

        // ✅ Lấy icon từ promotionClass (V2) chứ ko phải classData (V1)
        if (weaponIcon != null && classData.promotionClass != null && classData.promotionClass.classIcon != null)
            weaponIcon.sprite = classData.promotionClass.classIcon;
        else if (weaponIcon != null && classData.classIcon != null)
            weaponIcon.sprite = classData.classIcon;
    }

    public void SelectUpgrade()
    {
        if (currentChoice == null) return;

        switch (currentChoice.type)
        {
            case ChoiceType.Weapon:
                HandleWeaponChoice(currentChoice.weapon);
                break;

            case ChoiceType.SelectClass:
                HandleClassChoice(currentChoice.classData);
                break;

            case ChoiceType.Promotion:
                HandlePromotionChoice(currentChoice.classData);
                break;
        }

        if (UIController.instance != null)
            UIController.instance.UpdateActiveClassDisplay();

        // ✅ Close the active panel (level-up or promotion)
        if (UIController.instance != null)
        {
            if (UIController.instance.promotionPanel != null && UIController.instance.promotionPanel.activeInHierarchy)
                UIController.instance.promotionPanel.SetActive(false);
            else if (UIController.instance.levelUpPanel != null)
                UIController.instance.levelUpPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void HandleWeaponChoice(Weapon weapon)
    {
        if (weapon == null || PlayerController.instance == null) return;

        if (PlayerController.instance.assignedWeapons.Contains(weapon))
        {
            weapon.LevelUp();
        }
        else if (!PlayerController.instance.fullyLevelledWeapons.Contains(weapon))
        {
            PlayerController.instance.AddWeapon(weapon);
        }
    }

    private void HandleClassChoice(ClassData classData)
    {
        if (ClassManager.instance == null || classData == null) return;
        
        // ✅ Set firstClassSelected nếu chưa có (lần đầu chọn class từ level-up)
        if (ClassManager.instance.firstClassSelected == null)
        {
            ClassManager.instance.firstClassSelected = classData;
            Debug.Log($"✅ Set firstClassSelected = {classData.className}");
        }
        
        ClassManager.instance.ApplyClass(classData);
    }

    private void HandlePromotionChoice(ClassData classData)
    {
        if (classData == null || ClassManager.instance == null) return;
        ClassManager.instance.PromoteClass(classData);
    }
}