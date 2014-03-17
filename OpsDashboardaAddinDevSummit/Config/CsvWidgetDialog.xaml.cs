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

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;

namespace DataHandler.Config
{
  /// <summary>
  /// The Configure dialog of the widget
  /// </summary>
  public partial class CsvWidgetDialog : Window, INotifyPropertyChanged
  {
    private FieldHeadersInfo fieldHeadersInfo;

    public string Caption { get; private set; }
    public FieldHeadersInfo FieldHeadersInfo
    {
      get { return fieldHeadersInfo; }
      set
      {
        if (fieldHeadersInfo != value)
        {
          fieldHeadersInfo = value;
          OnPropertyChanged("FieldHeadersInfo");
        }
      }
    }
    public string CSVFilePath { get; set; }
    public string LongFieldName { get; set; }
    public string LatFieldName { get; set; }
    public string DisplayFieldName { get; set; }

    public CsvWidgetDialog(string caption, string csvFilePath, string latFieldName, string longFieldName, string displayFieldName)
    {
      InitializeComponent();

      //Show the Field settings until a .csv file is specified
      FieldHeadersInfo = new FieldHeadersInfo();
      FieldHeadersInfo.FieldsVisibility = Visibility.Hidden;

      // When re-configuring, initialize the widget config dialog using the previous settings.
      CaptionTextBox.Text = caption;
      CSVFilePath = string.IsNullOrEmpty(csvFilePath) ? string.Empty : csvFilePath;
      LatFieldName = latFieldName;
      LongFieldName = longFieldName;
      DisplayFieldName = displayFieldName;

      if (!string.IsNullOrEmpty(csvFilePath))
        FieldHeadersInfo = FieldHeadersInfo.CreateFieldHeaderinfo(CSVFilePath, this.LatFieldName, this.LongFieldName, this.DisplayFieldName);

      DataContext = this;
    }

    /// <summary>
    ///Browse button clicked 
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDlg = new OpenFileDialog();
      openFileDlg.DefaultExt = ".csv";

      Nullable<bool> result = openFileDlg.ShowDialog();
      if (result != true)
        return;

      //User specified a different csv file, we'll update the CSVFilePath property
      if (CSVFilePath != openFileDlg.FileName)
      {
        CSVFilePath = openFileDlg.FileName;
        txtCsvFileLct.Text = CSVFilePath;

        FieldHeadersInfo = FieldHeadersInfo.CreateFieldHeaderinfo(CSVFilePath);
      }
      else
        FieldHeadersInfo = FieldHeadersInfo.CreateFieldHeaderinfo(CSVFilePath, LatFieldName, LongFieldName, DisplayFieldName);
    }

    /// <summary>
    ///Ok button clicked 
    /// </summary>
    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      Caption = CaptionTextBox.Text;

      DialogResult = true;
    }

    #region INotifyPropertyChanged
    void OnPropertyChanged(string prop)
    {
      if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
