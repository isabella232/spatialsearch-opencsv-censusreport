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

using System;
using System.Collections.Generic;
using System.IO;

namespace DataHandler
{
  /// <summary>
  /// Helper class that read rows from a csv file
  /// </summary>
  public class Row : List<string>
  {
    public string Text { get; set; }
  }

  /// <summary>
  /// Class to read data from a CSV file
  /// </summary>
  class CsvParser : StreamReader
  {
    public CsvParser(string filename)
      : base(filename)
    {
    }

    private int RowLength;

    /// <summary>
    /// Reads a row of data from a CSV file
    /// </summary>
    public bool ReadOneRow(Row row)
    {
      row.Text = ReadLine();
      if (String.IsNullOrEmpty(row.Text))
        return false;

      //Clear items in the CsvRow
      row.Clear();
      RowLength = row.Text.Length;
      int currentIndex = 0;
      int startIndex = 0;
      string item;

      //Read to the end of the row's text
      while (IsNotEndOfLine(currentIndex))
      {
        //Text without quote
        if (row.Text[currentIndex] != '"')
        {
          startIndex = currentIndex;
          while (IsNotEndOfLine(currentIndex) && row.Text[currentIndex] != ',')
            currentIndex++;
        }

        // Found a quote character
        else if (row.Text[currentIndex] == '"')
        {
          // Skip the quote character
          currentIndex++;

          //Read the character after the quote
          startIndex = currentIndex;
          while (IsNotEndOfLine(currentIndex))
          {
            // Found another quote character
            if (row.Text[currentIndex] == '"')
            {
              //Break if end of line or the next character is a comma
              int nextIndex = currentIndex;
              ++nextIndex;
              if (!IsNotEndOfLine(nextIndex) || row.Text[nextIndex] == ',')
                break;
            }

            //Otherwise keep going
            currentIndex++;
          }
        }

        //Create the item, then add it to the row
        item = row.Text.Substring(startIndex, currentIndex - startIndex);
        row.Add(item);

        //The current index might be a quote character
        if (IsNotEndOfLine(currentIndex) && row.Text[currentIndex] == '\"')
          currentIndex++;
        //The current index might be a comma character
        if (IsNotEndOfLine(currentIndex) && row.Text[currentIndex] == ',')
          currentIndex++;
      }

      return true;
    }

    private bool IsNotEndOfLine(int currentIndex)
    {
      return currentIndex < RowLength;
    }

  }
}
