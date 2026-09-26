using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Filter
{
    /// <summary>
    /// Określa operator używany podczas filtrowania danych.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Równa się.
        /// Przykład: Age == 18
        /// </summary>
        Equals,

        /// <summary>
        /// Nie równa się.
        /// Przykład: Age != 18
        /// </summary>
        NotEquals,

        /// <summary>
        /// Zawiera określony tekst.
        /// Przykład: Name.Contains("Jan")
        /// </summary>
        Contains,

        /// <summary>
        /// Rozpoczyna się od określonego tekstu.
        /// Przykład: Name.StartsWith("Jan")
        /// </summary>
        StartsWith,

        /// <summary>
        /// Kończy się określonym tekstem.
        /// Przykład: Name.EndsWith("ski")
        /// </summary>
        EndsWith,

        /// <summary>
        /// Większe niż.
        /// Przykład: Age &gt; 18
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Większe lub równe.
        /// Przykład: Age &gt;= 18
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Mniejsze niż.
        /// Przykład: Age &lt; 18
        /// </summary>
        LessThan,

        /// <summary>
        /// Mniejsze lub równe.
        /// Przykład: Age &lt;= 18
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Wartość jest pusta (null).
        /// Przykład: Name == null
        /// </summary>
        IsNull,

        /// <summary>
        /// Wartość nie jest pusta (null).
        /// Przykład: Name != null
        /// </summary>
        IsNotNull
    }


}
