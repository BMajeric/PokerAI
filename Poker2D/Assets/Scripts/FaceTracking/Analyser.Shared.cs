using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public partial class Analyser : MonoBehaviour
{
    [Header("Analyser configuration settings")]
    public string dataPathAnalysis;

    [Header("Tracker object settings")]
    public Tracker Tracker;

    [Header("Analysis Data Element")]
    public GameObject AnalysisDataElement;

    [Header("Warning Panels")]
    public GameObject warningPanel;
    public Image warningPanelImage;

}

