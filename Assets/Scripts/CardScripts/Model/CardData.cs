using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class CardData
{
    public string id;
    public string name;
    public int attack;
    public int health;
    public int cost;
    public string rarity;
    public string description;
    public string imageName;
}

[Serializable]
public class CardDataList
{
    public CardData[] cards;
}

public class JsonToSOImporter : EditorWindow
{
    // Œcie¿ka do JSON-a z 70 kartami
    string jsonPath = "Assets/Resources/Data/cards_skydeck.json";

    // Folder, gdzie powstan¹ pliki .asset
    string outputFolder = "Assets/Resources/Cards";

    // Folder z grafikami wewn¹trz Resources (np. Assets/Resources/CardArt)
    string artResourcesFolder = "Assets/Resources/Cards/Card Images";

    [MenuItem("Sky Deck/Import Cards JSON to SOs")]
    static void Init()
    {
        var window = (JsonToSOImporter)EditorWindow.GetWindow(typeof(JsonToSOImporter));
        window.titleContent = new GUIContent("Sky Deck Card Importer");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("JSON ? CardSO (Sky Deck: Beyond The Szpont)", EditorStyles.boldLabel);

        jsonPath = EditorGUILayout.TextField("JSON Path:", jsonPath);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        artResourcesFolder = EditorGUILayout.TextField("Art Resources Folder:", artResourcesFolder);

        if (GUILayout.Button("Importuj karty"))
        {
            Import();
        }

        if (GUILayout.Button("Do³aduj same obrazki do istniej¹cych assetów"))
        {
            LoadImagesToExistingAssets();
        }
    }

    void Import()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError("Nie znaleziono pliku JSON pod œcie¿k¹: " + jsonPath);
            return;
        }

        string jsonText = File.ReadAllText(jsonPath);
        CardDataList dataList = JsonUtility.FromJson<CardDataList>(jsonText);

        if (dataList == null || dataList.cards == null || dataList.cards.Length == 0)
        {
            Debug.LogError("Nie uda³o siê sparsowaæ JSON-a lub brak kart w pliku.");
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var card in dataList.cards)
        {
            var so = ScriptableObject.CreateInstance<CardSO>();

            // mapowanie danych
            so.id = card.id;
            so.cardName = card.name;
            so.attack = card.attack;
            so.health = card.health;
            so.cost = card.cost;
            so.description = card.description;

            // próba podpiêcia obrazka, jeœli imageName jest ustawione
            if (!string.IsNullOrEmpty(card.imageName))
            {
                string resPath = artResourcesFolder + "/" + card.imageName; // np. "CardArt/adder"
                Texture2D texture = Resources.Load<Texture2D>(resPath);

                if (texture == null)
                {
                    Debug.LogWarning($"[Sky Deck] Brak sprite'a dla karty '{card.id}' w Resources path: {resPath}");
                }
                else
                {
                    so.artwork = texture;
                }
            }

            string safeId = string.IsNullOrWhiteSpace(card.id) ? Guid.NewGuid().ToString() : card.id;
            string assetPath = $"{outputFolder}/{safeId}.asset";

            AssetDatabase.CreateAsset(so, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Sky Deck] DONE — zaimportowano {dataList.cards.Length} kart z JSON-a.");
    }

    void LoadImagesToExistingAssets()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError("Nie znaleziono pliku JSON: " + jsonPath);
            return;
        }

        string jsonText = File.ReadAllText(jsonPath);
        CardDataList dataList = JsonUtility.FromJson<CardDataList>(jsonText);

        if (dataList == null || dataList.cards == null)
        {
            Debug.LogError("B³¹d parsowania JSON-a.");
            return;
        }

        int assigned = 0;

        foreach (var card in dataList.cards)
        {
            if (string.IsNullOrEmpty(card.id) || string.IsNullOrEmpty(card.imageName))
                continue;

            string assetPath = $"{outputFolder}/{card.id}.asset";
            CardSO so = AssetDatabase.LoadAssetAtPath<CardSO>(assetPath);

            if (so == null)
            {
                Debug.LogWarning($"[Sky Deck] Nie znaleziono assetu dla karty: {card.id}");
                continue;
            }

            if (so.artwork != null)
            {
                // jeœli chcesz nadpisywaæ, usuñ ten if
                continue;
            }

            string resPath = $"{artResourcesFolder}/{card.imageName}";
            Texture2D texture = Resources.Load<Texture2D>(resPath);

            if (texture == null)
            {
                Debug.LogWarning($"[Sky Deck] Brak tekstury PNG: {resPath}");
                continue;
            }

            so.artwork = texture;
            EditorUtility.SetDirty(so);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Sky Deck] DONE — przypisano artwork do {assigned} kart.");
    }
}