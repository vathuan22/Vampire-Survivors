using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance; // Create a singleton instance of the PlayerController. GK
    public SpriteRenderer spriteRenderer;
    public void Awake()
    {
        instance = this; // Set the instance to this object. GK
    }
    public float moveSpeed; // The speed at which the player moves. AK

    public Animator anim; // The animator component. AK
    public float pickupRange = 1.5f; // The range of the attack. AK
    //public Weapon activeWeapon; // The active weapon. GK
    public List<Weapon> unassignedWeapons, assignedWeapons; // The list of unassigned and assigned weapons. GK
    public int maxWeapons = 3; // The maximum number of weapons. GK
    [HideInInspector]
    public List<Weapon> fullyLevelledWeapons = new List<Weapon>(); // The list of fully levelled weapons. GK

    void Start()
    {
        if(assignedWeapons.Count == 0) // If the assigned weapons count is 0. GK
        {        
            AddWeapon(Random.Range(0, unassignedWeapons.Count)); // Add a random weapon. GK

        }
        moveSpeed = PlayerStatController.instance.moveSpeed[0].value; // Set the move speed to the player stat controller move speed. GK
        pickupRange = PlayerStatController.instance.pickupRange[0].value; // Set the pickup range to the player stat controller pickup range. GK
        maxWeapons = Mathf.RoundToInt(PlayerStatController.instance.maxWeapons[0].value); // Set the max weapons to the player stat controller max weapons. GK
    }

    void Update()
    {
        Vector3 moveInput = new Vector3(0f, 0f, 0f);
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        moveInput.Normalize();

        transform.position += moveInput * moveSpeed * Time.deltaTime;

        // --- PHẦN SỬA ĐỂ QUAY TRÁI QUAY PHẢI ---
        if (moveInput.x > 0) // Di chuyển sang phải
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput.x < 0) // Di chuyển sang trái
        {
            spriteRenderer.flipX = true;
        }
        // ---------------------------------------

        if (moveInput != Vector3.zero)
        {
            anim.SetBool("isMoving", true);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
    }
    public void AddWeapon(int weaponNumber) // Function to add a weapon. GK
    {
        if (weaponNumber < unassignedWeapons.Count) // If the weapon number is less than the unassigned weapons count. GK  
        {
            assignedWeapons.Add(unassignedWeapons[weaponNumber]); // Add the weapon to the assigned weapons. GK
            unassignedWeapons[weaponNumber].gameObject.SetActive(true); // Set the weapon to active. GK
            unassignedWeapons.RemoveAt(weaponNumber); // Remove the weapon from the unassigned weapons. GK
        }
    }
    public void AddWeapon(Weapon weaponToAdd) // Function to add the weapon. GK
    {
        weaponToAdd.gameObject.SetActive(true); // Set the weapon to active. GK
        assignedWeapons.Add(weaponToAdd); // Add the weapon to the assigned weapons. GK
        unassignedWeapons.Remove(weaponToAdd); // Remove the weapon from the unassigned weapons. GK
    }
}