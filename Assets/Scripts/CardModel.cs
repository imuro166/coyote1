using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardModel : MonoBehaviour
{
    Image image;

    public Sprite[] faces;
    public Sprite cardBack;

    public int cardIndex;

    public void ToggleFace(bool showFace)
    {
        if (showFace)
        {
            image.sprite = faces[cardIndex];
        }
        else {
            image.sprite = cardBack;
        }
    }

    private void Awake()
    {
        image = GetComponent<Image>();
    }
}
