using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BallSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TMP_Text selectedBallText;

    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Slot Settings")]
    [SerializeField] private int baseSlotCapacity = 5;

    private List<Image> slotImages = new List<Image>();
    private List<GameObject> slots = new List<GameObject>();
    private int currentBallCount = 0;

    void Start()
    {
        GameEvents.OnSelectedBallChanged += OnSelectedBallChanged; // Subscribe to the selected ball changed event only

        InitializeEmptySlots(); // Initialize with empty slots
    }

    void OnDestroy()
    {
        GameEvents.OnSelectedBallChanged -= OnSelectedBallChanged;
    }

    void InitializeEmptySlots()
    {
        // Clear existing slots
        foreach (var slot in slots)
            Destroy(slot);
        slots.Clear();
        slotImages.Clear();

        // Create empty slots based on base capacity
        for (int i = 0; i < baseSlotCapacity; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);
            slots.Add(slot);

            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                slotImages.Add(img);
                img.color = emptyColor; // Set all slots to empty color initially
            }
        }
    }

    void OnSelectedBallChanged(int slotIndex)
    {
        UpdateSlotVisuals(slotIndex); // Update visual representation based on selected ball
        UpdateSelectedBallText(slotIndex); // Update the selected ball text display
    }

    void UpdateSlotVisuals(int selectedSlot)
    {
        var inv = PlayerInventory.Instance;
        if (inv == null) return;

        currentBallCount = inv.UsedBallSlots;        
        for (int i = 0; i < slotImages.Count; i++) // Update slot visuals based on available balls and selection
        {
            if (i < currentBallCount)
            {
                if (i == selectedSlot) { slotImages[i].color = selectedColor; }
                else { slotImages[i].color = normalColor; }
            }
            else { slotImages[i].color = emptyColor; }
        }
        if (currentBallCount > baseSlotCapacity) // If we need more slots than the base capacity, create them dynamically
        {
            CreateAdditionalSlots(currentBallCount - baseSlotCapacity); 
            UpdateSlotVisuals(selectedSlot); // Recursively update visuals with the new slots
        }
    }

    void CreateAdditionalSlots(int additionalCount)
    {
        for (int i = 0; i < additionalCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);
            slots.Add(slot);

            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                slotImages.Add(img);
            }
        }

        // Update base capacity to new slot count
        baseSlotCapacity = slots.Count;
    }

    void UpdateSelectedBallText(int slotIndex)
    {
        if (selectedBallText == null) return;

        var inv = PlayerInventory.Instance;
        if (inv != null && slotIndex < inv.UsedBallSlots)
        {
            var ballData = inv.GetBallInstanceForLaunch(slotIndex);
            if (ballData != null && !string.IsNullOrEmpty(ballData.BallName))
            {
                selectedBallText.text = $"Selected: {ballData.BallName}";
            }
            else
            {
                selectedBallText.text = $"Selected: Ball {slotIndex + 1}";
            }
        }
        else if (inv != null)
        {
            selectedBallText.text = $"Selected: None";
        }
    }

    // Optional: Public method to manually refresh if needed
    public void RefreshSlots()
    {
        var inv = PlayerInventory.Instance;
        if (inv != null && inv.UsedBallSlots > 0)
        {
            // Get current selected slot from BallManager
            var ballManager = BallManager.Instance;
            if (ballManager != null)
            {
                int currentSelected = ballManager.GetSelectedSlot;
                OnSelectedBallChanged(currentSelected);
            }
        }
    }
}