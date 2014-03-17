//Copyright 2014 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using ESRI.ArcGIS.Client;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SelectByLocation
{
  public partial class SelectByLocationDialog : Window, INotifyPropertyChanged
  {
    public IList<FeatureLayer> SelectableLayers { get; set; }

    private IList<FeatureLayer> targetLayers;
    public IList<FeatureLayer> TargetLayers
    {
      get { return targetLayers; }
      set
      {
        if (targetLayers != value)
        {
          targetLayers = value;
          OnPropertyChanged("TargetLayers");
        }
      }
    }
    private CheckableItem<FeatureLayer>[] checkableLayers;
    public CheckableItem<FeatureLayer>[] CheckableLayers
    {
      get { return checkableLayers; }
      set
      {
        if (checkableLayers != value)
        {
          checkableLayers = value;
          OnPropertyChanged("CheckableLayers");
        }
      }
    }

    private FeatureLayer sourceLayer;
    public FeatureLayer SourceLayer
    {
      get { return sourceLayer; }
      set
      {
        if (sourceLayer != value)
        {
          sourceLayer = value;
          OnPropertyChanged("SourceLayer");
        }
      }
    }

    /// <summary>
    /// Create a SelectByLocationDialog by passing the previously selected target layers and source layers, as well as all currently selectable layers
    /// </summary>
    public SelectByLocationDialog(IList<FeatureLayer> SelectableLayers, IList<FeatureLayer> LastTargetLayers, FeatureLayer LastSourceLayer)
    {
      InitializeComponent();

      //All selectable layers
      this.SelectableLayers = SelectableLayers;

      //Target Layers
      //If a layer is in SelectedTargetLayers but it's not in SelectableLayers, exclude it 
      if (LastTargetLayers != null)
        TargetLayers = LastTargetLayers.Where(lyr => SelectableLayers.Contains(lyr)).ToList();
      else
        TargetLayers = new List<FeatureLayer>();

      //Create checkable layers for binding 
      CreateCheckableLayers();

      //Source Layer
      if (LastSourceLayer != null)
        SourceLayer = LastSourceLayer;
      else if (SelectableLayers.Count > 0)
        SourceLayer = SelectableLayers[0];

      DataContext = this;
    }

    /// <summary>
    /// Create an array of CheckableLayers to be displayed in the UI. 
    /// When op. view author finishes configuring, the checked items will be used to update the
    /// list of TargetLayers (see btnConfigOK_Click())
    /// </summary>
    private void CreateCheckableLayers()
    {
      List<CheckableItem<FeatureLayer>> CheckableLayerList = new List<CheckableItem<FeatureLayer>>();
      bool hasTargetLayers = TargetLayers == null || TargetLayers.Count == 0 ? false : true;

      foreach (FeatureLayer layer in SelectableLayers)
      {
        CheckableItem<FeatureLayer> checkableLayer = new CheckableItem<FeatureLayer>(layer);
        if (!hasTargetLayers)
          checkableLayer.IsChecked = false;
        else if (TargetLayers.Contains(layer))
          checkableLayer.IsChecked = true;
        else
          checkableLayer.IsChecked = false;

        CheckableLayerList.Add(checkableLayer);
      }

      CheckableLayers = CheckableLayerList.ToArray();
    }

    private void btnConfigOK_Click(object sender, RoutedEventArgs e)
    {
      if (CheckableLayers.Count(item => item.IsChecked) == 0)
      {
        MessageBox.Show("Select at least one target layer", "No target layer");
        DialogResult = false;
      }

      TargetLayers = (from item in CheckableLayers where item.IsChecked == true select item.Item).ToList<FeatureLayer>();

      DialogResult = true;
    }

    private void btnConfigCancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      this.Close();
    }

    #region INotifyPropertyChanged
    void OnPropertyChanged(string prop)
    {
      if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }
    public event PropertyChangedEventHandler PropertyChanged; 
    #endregion

  }

  /// <summary>
  /// Helper class used by CheckableLayerList
  /// </summary>
  public class CheckableItem<T> : INotifyPropertyChanged
  {
    public T Item { get; set; }

    private bool isChecked;
    public bool IsChecked
    {
      get { return isChecked; }
      set
      {
        if (isChecked != value)
        {
          isChecked = value;
          RaisePropertyChanged("IsChecked");
        }
      }
    }

    public CheckableItem(T item)
    {
      Item = item;
    }

    void RaisePropertyChanged(string prop)
    {
      if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }
    public event PropertyChangedEventHandler PropertyChanged;
  }
}

