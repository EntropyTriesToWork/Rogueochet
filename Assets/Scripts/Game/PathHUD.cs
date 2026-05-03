using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathHUD : MonoBehaviour
{
    [Header("Container")]
    public Transform RoomIconsContainer;
    public GameObject RoomIconPrefab;
    public CanvasGroup PathCanvasGroup;

    [Header("Sprites")]
    public Sprite CombatSprite;
    public Sprite ShopSprite;
    public Sprite EventSprite;

    private List<Image> _icons = new List<Image>();
    private List<Image> _selectionImages = new List<Image>();
    private List<TextMeshProUGUI> _waveTexts = new List<TextMeshProUGUI>();

    void Start()
    {
        GameEvents.OnGameStarted += OnGameStarted;
        GameEvents.OnRoomCleared += OnRoomCleared;
    }

    void OnDestroy()
    {
        GameEvents.OnGameStarted -= OnGameStarted;
        GameEvents.OnRoomCleared -= OnRoomCleared;
    }

    void OnGameStarted()
    {
        GenerateUI();
    }

    void OnRoomCleared()
    {
        UpdateRoomHighlight(GameManager.Instance.CurrentRoomIndex); // Move highlight to next room
    }

    void GenerateUI()
    {
        foreach (Transform child in RoomIconsContainer)
            Destroy(child.gameObject);
        _icons.Clear();
        _waveTexts.Clear();

        var rooms = PathManager.Instance?.GetAllRooms();
        if (rooms == null) return;

        for (int i = 0; i < rooms.Count; i++)
        {
            GameObject iconObj = Instantiate(RoomIconPrefab, RoomIconsContainer);
            Image selectImg = iconObj.GetComponent<Image>();
            _selectionImages.Add(selectImg);
            Image img = iconObj.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite = GetSpriteForType(rooms[i].Type);
                _icons.Add(img);
            }
        }

        UpdateRoomHighlight(0); // start at first room
    }
    Sprite GetSpriteForType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Combat: return CombatSprite;
            case RoomType.Shop: return ShopSprite;
            default: return EventSprite;
        }
    }

    void UpdateRoomHighlight(int currentIndex)
    {
        for (int i = 0; i < _selectionImages.Count; i++)
        {
            _selectionImages[i].enabled = i == currentIndex;
            _icons[i].color = i == currentIndex ? Color.white : Color.gray;
        }

    }
}