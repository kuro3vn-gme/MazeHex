using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazeSettings : MonoBehaviour
{
    [SerializeField]
    private Slider wallWidthSlider;

    [SerializeField]
    private Slider wallHeightSlider;

    [SerializeField]
    private Slider hexRadiusSlider;

    [SerializeField]
    private Slider mapSizeSlider;

    [SerializeField]
    private MazeHex mazeHex;

	private void Awake()
	{
        wallWidthSlider.value = mazeHex.wallWidth;
        wallHeightSlider.value = mazeHex.wallHeight;
        hexRadiusSlider.value = mazeHex.hexRadius;
        mapSizeSlider.value = mazeHex.mapSize;
	}

    public void OnWallWidthSliderChanged(float val)
	{
        if (!mazeHex.IsInitialized) return;
        mazeHex.wallWidth = val;
        mazeHex.CreateHexMap();
        mazeHex.CreateMaze();
	}

    public void OnWallHeightSliderChanged(float val)
	{
        if (!mazeHex.IsInitialized) return;
        mazeHex.wallHeight = val;
        mazeHex.CreateHexMap();
        mazeHex.CreateMaze();
	}

    public void OnHexRadiusSliderChanged(float val)
	{
        if (!mazeHex.IsInitialized) return;
        mazeHex.hexRadius = val;
        mazeHex.CreateHexMap();
        mazeHex.CreateMaze();
	}

    public void OnRangeSliderChanged(float val)
	{
        if (!mazeHex.IsInitialized) return;
        mazeHex.mapSize = (int)val;
        mazeHex.CreateHexMap();
        mazeHex.CreateMaze();
	}
}
