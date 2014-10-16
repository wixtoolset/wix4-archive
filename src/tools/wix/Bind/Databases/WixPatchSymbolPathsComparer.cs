//-------------------------------------------------------------------------------------------------
// <copyright file="WixPatchSymbolPathsComparer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// Sorts the WixPatchSymbolPaths table for processing.
    /// </summary>
    internal sealed class WixPatchSymbolPathsComparer : IComparer<Row>
    {
        /// <summary>
        /// Compares two rows from the WixPatchSymbolPaths table.
        /// </summary>
        /// <param name="a">First row to compare.</param>
        /// <param name="b">Second row to compare.</param>
        /// <remarks>Only the File, Product, Component, Directory, and Media tables links are allowed by this method.</remarks>
        /// <returns>Less than zero if a is less than b; Zero if they are equal, and Greater than zero if a is greater than b</returns>
        //public int Compare(Object a, Object b)
        //{
        //    Row ra = (Row)a;
        //    Row rb = (Row)b;

        //    SymbolPathType ia = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)ra.Fields[0]).Data.ToString());
        //    SymbolPathType ib = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)rb.Fields[0]).Data.ToString());
        //    return (int)ib - (int)ia;
        //}

        public int Compare(Row x, Row y)
        {
            SymbolPathType ix = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), x.FieldAsString(0));
            SymbolPathType iy = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), y.FieldAsString(0));

            return (int)iy - (int)ix;
        }

        /// <summary>
        /// The types that the WixPatchSymbolPaths table can hold (and that the WixPatchSymbolPathsComparer can sort).
        /// </summary>
        private enum SymbolPathType
        {
            File,
            Component,
            Directory,
            Media,
            Product
        };
    }
}
