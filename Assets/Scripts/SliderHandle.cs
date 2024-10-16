using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandle : MonoBehaviour
{
    public Slider _slider;
    public BoardState boardState;
    
    void Start() {
        _slider.onValueChanged.AddListener((v) => {
            boardState.updateInterval = 1.0f / v;
        });
    }
}
