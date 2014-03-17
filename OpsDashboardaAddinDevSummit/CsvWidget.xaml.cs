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

using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using client = ESRI.ArcGIS.Client;

namespace DataHandler
{
  /// <summary>
  /// List features from a user-specified csv file in an attribute table.
  /// Optionally, draw the features on the map. 
  /// When user hovers over a feature, a map tip will show 
  /// 
  /// Note: 
  /// 1. The .csv file must be comma-separated
  /// 2. The location fields of the .csv file are assumed to be WGS84 based. i.e. The field should have a latitude field and a longitude field
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
  [ExportMetadata("DisplayName", "CSV Widget")]
  [ExportMetadata("Description", "List the features from a CSV file in an attribute table and optionally draw them on the map")]
  [ExportMetadata("ImagePath", "/OpsDashboardaAddinDevSummit;component/Images/csv.png")]
  [DataContract]
  public partial class CsvWidget : UserControl, IWidget
  {
    public List<Feature> Features { get; set; }

    client.GraphicsLayer graphicsLayers = null;

    public FieldHeadersInfo FieldHeadersInfo { get; set; }

    [DataMember(Name = "Long")]
    public string LongFieldName { get; set; }

    [DataMember(Name = "Lat")]
    public string LatFieldName { get; set; }

    [DataMember(Name = "Csv")]
    public string CsvFilePath { get; set; }

    [DataMember(Name = "DisplayField")]
    public string DisplayFieldName { get; set; }

    public CsvWidget()
    {
      InitializeComponent();

      DataContext = this;
    }

    #region IWidget Members
    private string _caption = "Default Caption";
    [DataMember(Name = "caption")]
    public string Caption
    {
      get
      {
        return _caption;
      }

      set
      {
        if (value != _caption)
        {
          _caption = value;
        }
      }
    }

    [DataMember(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// When the widget is being inactivated, we try to load the .csv file using CsvFilePath
    /// </summary>
    public void OnActivated()
    {
      Features = new List<Feature>();

      if (string.IsNullOrEmpty(LatFieldName) || string.IsNullOrEmpty(LongFieldName) || string.IsNullOrEmpty(CsvFilePath))
        return;

      if (!File.Exists(CsvFilePath))
      {
        CsvFilePath = string.Empty;
        return;
      } 

      //Get the field header info so that we can set up the attributes table
      FieldHeadersInfo = FieldHeadersInfo.CreateFieldHeaderinfo(CsvFilePath, LatFieldName, LongFieldName, DisplayFieldName);
      SetUpFeatureGrid();
      PopulateFeatureGrid();
    }

    /// <summary>
    /// When the widget is being deactivated, we try to remove the temporary graphics layer which represents the 
    /// records from the .csv files
    /// </summary>
    public void OnDeactivated()
    {
      FieldHeadersInfo = null;
      Features = null;

      RemoveFeaturesFromMap();
    }

    public bool CanConfigure
    {
      get { return true; }
    }

    public bool Configure(Window owner, IList<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources)
    {
      // Show the configuration dialog.
      Config.CsvWidgetDialog dialog = new Config.CsvWidgetDialog(Caption, CsvFilePath, LatFieldName, LongFieldName, DisplayFieldName) { Owner = owner };
      if (dialog.ShowDialog() != true)
        return false;

      // Retrieve the selected values for the properties from the configuration dialog.
      Caption = dialog.Caption;
      FieldHeadersInfo = dialog.FieldHeadersInfo;
      CsvFilePath = dialog.CSVFilePath;

      //Save setting to variables which will then be serialized
      LongFieldName = FieldHeadersInfo.SelectedLongField.FieldName;
      LatFieldName = FieldHeadersInfo.SelectedLatField.FieldName;
      DisplayFieldName = FieldHeadersInfo.SelectedDisplayField.FieldName;

      //Show data
      SetUpFeatureGrid();
      PopulateFeatureGrid();

      //Draw the records from the .csv file on the map if the op view user has already checked the DrawFeatures box
      if (DrawFeatures.IsChecked == true)
        AddFeaturesToMap();

      return true;
    }
    #endregion

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
      AddFeaturesToMap();
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      RemoveFeaturesFromMap();
    }

    #region Helper methods
    /// <summary>
    /// Add columns and set up bindings for each column in the data grid
    /// </summary>
    private void SetUpFeatureGrid()
    {
      FeatureGrid.Columns.Clear();
      foreach (FieldInfo field in FieldHeadersInfo.Fields)
        FeatureGrid.Columns.Add(new DataGridTextColumn()
        {
          Binding = new Binding("Attributes [" + field.FieldName + "]"),
          Header = field.FieldName
        });
    }

    /// <summary>
    /// Populate FeatureGrid from Features
    /// </summary>
    private void PopulateFeatureGrid()
    {
      ParseCsvData();

      FeatureGrid.ItemsSource = Features;
    }

    /// <summary>
    /// Create features from the csv file
    /// </summary>
    private void ParseCsvData()
    {
      Features = new List<Feature>();

      Row row = new Row();
      try
      {
        using (CsvParser reader = new CsvParser(CsvFilePath))
        {
          //Read off the first line
          if (!reader.ReadOneRow(row))
            return;

          //Read a row
          while (reader.ReadOneRow(row))
          {
            //Read the fields
            IDictionary<string, object> Attributes = new Dictionary<string, object>();
            for (int i = 0; i < row.Count(); i++)
              Attributes.Add(FieldHeadersInfo.Fields[i].FieldName, row[i].ToString());

            Feature feature = new Feature(Attributes);

            Features.Add(feature);
          }
        }
      }
      catch (IOException)
      {
        MessageBox.Show("Error reading csv file");
      }
      catch (Exception)
      {
        MessageBox.Show("Error occured");
      }
    }

    /// <summary>
    /// Draw the features on map using a temporary GraphicsLayer
    /// </summary>
    private void AddFeaturesToMap()
    {
      if (FieldHeadersInfo == null || Features == null || Features.Count == 0)
        return;

      //note: we will use the first map widget in the view
      //developers can update the logic to pick another map widget to display the layer
      MapWidget mapWidget = OperationsDashboard.Instance.Widgets.FirstOrDefault(w => w.GetType() == typeof(MapWidget)) as MapWidget;
      if (mapWidget == null || mapWidget.Map == null || !mapWidget.Map.IsInitialized)
        return;

      //Get the lat field, long field and display field to help us create the graphics
      FieldInfo latField = FieldHeadersInfo.SelectedLatField;
      FieldInfo longField = FieldHeadersInfo.SelectedLongField;
      FieldInfo displayField = FieldHeadersInfo.SelectedDisplayField;

      //Create a Graphics object which will contain the graphics representing the records
      Graphics graphics = new Graphics();
      graphics.ConvertFeaturesToGraphics(Features, latField, longField, displayField);

      //Create a GraphicsLayer which point the GraphicsSource to the graphics object
      graphicsLayers = new client.GraphicsLayer();
      graphicsLayers.ID = "tempGraphicsLyr" + System.Guid.NewGuid();
      graphicsLayers.GraphicsSource = graphics;
      graphicsLayers.Visible = true;

      //Add the GraphicsLayer to the map
      mapWidget.Map.Layers.Add(graphicsLayers);
    }

    /// <summary>
    /// Remove the graphics layer when the widget is removed
    /// </summary>
    private void RemoveFeaturesFromMap()
    {
      MapWidget mapWidget = OperationsDashboard.Instance.Widgets.FirstOrDefault(w => w.GetType() == typeof(MapWidget)) as MapWidget;
      if (mapWidget == null || mapWidget.Map == null || !mapWidget.Map.IsInitialized || graphicsLayers == null)
        return;

      var Layers = mapWidget.Map.Layers;
      var oldGraphicsLyr = Layers.FirstOrDefault(lyr => lyr.ID == graphicsLayers.ID);
      if (oldGraphicsLyr != null)
        mapWidget.Map.Layers.Remove(oldGraphicsLyr);
    } 
    #endregion
  }
}
