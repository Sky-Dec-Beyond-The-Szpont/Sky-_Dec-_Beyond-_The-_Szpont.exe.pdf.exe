//using System;
//using System.IO;
//using UnityEditor;
//using UnityEngine;

//[Serializable]
//public class CardData
//{
//    public string id;
//    public string name;
//    public int attack;
//    public int health;
//    public int cost;
//    public string rarity;
//    public string description;
//}

//[Serializable]
//public class CardDataList
//{
//    public CardData[] cards;
//}

//public class JsonToSOImporter : EditorWindow
//{
//    string jsonPath = "Assets/Resources/Data/cards.json";
//    string outputFolder = "Assets/Resources/Cards";

//    [MenuItem("Tools/Import Cards JSON to SOs")]
//    static void Init()
//    {
//        JsonToSOImporter window = (JsonToSOImporter)EditorWindow.GetWindow(typeof(JsonToSOImporter));
//        window.Show();
//    }

//    void OnGUI()
//    {
//        GUILayout.Label("JSON to ScriptableObject Importer", EditorStyles.boldLabel);

//        jsonPath = EditorGUILayout.TextField("JSON Path:", jsonPath);
//        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);

//        if (GUILayout.Button("Import"))
//        {
//            Import();
//        }
//    }

//    void Import()
//    {
//        // wczytaj JSON
//        string jsonText = File.ReadAllText(jsonPath);

//        CardDataList dataList = JsonUtility.FromJson<CardDataList>(jsonText);

//        if (!Directory.Exists(outputFolder))
//            Directory.CreateDirectory(outputFolder);

//        foreach (var card in dataList.cards)
//        {
//            var so = ScriptableObject.CreateInstance<CardSO>();

//            so.id = card.id;
//            so.cardName = card.name;
//            so.attack = card.attack;
//            so.health = card.health;
//            so.cost = card.cost;
//            so.description = card.description;

//            string assetPath = $"{outputFolder}/{card.id}.asset";
//            AssetDatabase.CreateAsset(so, assetPath);
//        }

//        AssetDatabase.SaveAssets();
//        AssetDatabase.Refresh();

//        Debug.Log("DONE — imported " + dataList.cards.Length + " cards");
//    }
//}