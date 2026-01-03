using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CaseCell : MonoBehaviour
{
    [System.Serializable]
    private class ListOfSprites
    {
        public List<Sprite> Sprites;
    }

    [SerializeField] private List<ListOfSprites> _sprites;
    [SerializeField] private int[] _chances;
    [SerializeField] private Color[] _colors;

    public int SelectedIndex { get; private set; }
    public Sprite SelectedSprite { get; private set; }

    public void Setup()
    {
        SelectedIndex = Randomize();

        SelectedSprite =
            _sprites[SelectedIndex].Sprites[Random.Range(0, _sprites[SelectedIndex].Sprites.Count)];

        GetComponent<Image>().sprite = SelectedSprite;

        transform.parent.GetComponent<Image>().color = _colors[SelectedIndex];
    }

    private int Randomize()
    {
        int total = 0;
        foreach (var c in _chances)
            total += c;

        int rand = Random.Range(0, total);
        int current = 0;

        for (int i = 0; i < _chances.Length; i++)
        {
            current += _chances[i];
            if (rand < current)
                return i;
        }

        return _chances.Length - 1;
    }
}
