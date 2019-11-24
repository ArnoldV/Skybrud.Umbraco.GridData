﻿using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skybrud.Essentials.Json.Extensions;
using Skybrud.Umbraco.GridData.Factories;

namespace Skybrud.Umbraco.GridData.Models {

    /// <summary>
    /// Class representing a row in an Umbraco Grid.
    /// </summary>
    public class GridRow : GridElement, IGridRow {

        #region Properties

        /// <summary>
        /// Gets a reference to the parent <see cref="IGridSection"/>.
        /// </summary>
        [JsonIgnore]
        public IGridSection Section { get; }

        /// <summary>
        /// Gets the unique ID of the row.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the label of the row. Use <see cref="HasLabel"/> to check whether a label has been specified.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets whether a label has been specified for the definition of this row.
        /// </summary>
        [JsonIgnore]
        public bool HasLabel => !string.IsNullOrWhiteSpace(Label);

        /// <summary>
        /// Gets the name of the row.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets an array of all areas in the row.
        /// </summary>
        public IGridArea[] Areas { get; }

        /// <summary>
        /// Gets a reference to the previous row.
        /// </summary>
        [JsonIgnore]
        public IGridRow PreviousRow { get; internal set; }

        /// <summary>
        /// Gets a reference to the next row.
        /// </summary>
        [JsonIgnore]
        public IGridRow NextRow { get; internal set; }

        /// <summary>
        /// Gets whether the row has any areas.
        /// </summary>
        [JsonIgnore]
        public bool HasAreas => Areas.Length > 0;

        /// <summary>
        /// Gets the first area of the row. If the row doesn't contain any areas, this property will return <c>null</c>.
        /// </summary>
        [JsonIgnore]
        public IGridArea FirstRow => Areas.FirstOrDefault();

        /// <summary>
        /// Gets the last area of the row. If the row doesn't contain any areas, this property will return <c>null</c>.
        /// </summary>
        [JsonIgnore]
        public IGridArea LastRow => Areas.LastOrDefault();

        /// <summary>
        /// Gets whether at least one area or control within the row is valid.
        /// </summary>
        [JsonIgnore]
        public override bool IsValid {
            get { return Areas.Any(x => x.IsValid); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance based on the specified <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">An instance of <see cref="JObject"/> representing the row.</param>
        protected GridRow(JObject obj) : base(obj) { }

        /// <summary>
        /// Initializes a new instance based on the specified <paramref name="json"/> object, <paramref name="section"/> and <paramref name="factory"/>.
        /// </summary>
        /// <param name="json">An instance of <see cref="JObject"/> representing the section.</param>
        /// <param name="section">The parent section.</param>
        /// <param name="factory">The factory used for parsing subsequent parts of the grid.</param>
        public GridRow(JObject json, IGridSection section, IGridFactory factory) : base(json) {

            Section = section;
            Id = json.GetString("id");
            Label = json.GetString("label");
            Name = json.GetString("name");

            Areas = json.GetArray("areas", x => factory.CreateGridArea(x, this)) ?? new IGridArea[0];

            // Update "PreviousArea" and "NextArea" properties
            for (int i = 1; i < Areas.Length; i++) {
                // TODO: Due to the factory, we can no longer assume rows are GridArea
                ((GridArea) Areas[i - 1]).NextArea = Areas[i];
                ((GridArea) Areas[i]).PreviousArea = Areas[i - 1];
            }

        }

        #endregion

        #region Member methods

        /// <summary>
        /// Gets an array of all nested controls. 
        /// </summary>
        public IGridControl[] GetAllControls() {
            return (
                from area in Areas
                from control in area.Controls
                select control
            ).ToArray();
        }

        /// <summary>
        /// Gets an array of all nested controls with the specified editor <paramref name="alias"/>. 
        /// </summary>
        /// <param name="alias">The editor alias of controls to be returned.</param>
        public IGridControl[] GetAllControls(string alias) {
            return GetAllControls(x => x.Editor.Alias == alias);
        }

        /// <summary>
        /// Gets an array of all nested controls matching the specified <paramref name="predicate"/>. 
        /// </summary>
        /// <param name="predicate">The predicate (callback function) used for comparison.</param>
        public IGridControl[] GetAllControls(Func<IGridControl, bool> predicate) {
            return (
                from area in Areas
                from control in area.Controls
                where predicate(control)
                select control
            ).ToArray();
        }

        /// <summary>
        /// Generates the HTML for the Grid row.
        /// </summary>
        /// <param name="helper">The <see cref="HtmlHelper"/> used for rendering the Grid row.</param>
        /// <returns>Returns the Grid row as an instance of <see cref="HtmlString"/>.</returns>
        public HtmlString GetHtml(HtmlHelper helper) {
            return GetHtml(helper, Name);
        }

        /// <summary>
        /// Generates the HTML for the Grid row.
        /// </summary>
        /// <param name="helper">The <see cref="HtmlHelper"/> used for rendering the Grid row.</param>
        /// <param name="partial">The alias or virtual path to the partial view for rendering the Grid row.</param>
        /// <returns>Returns the Grid row as an instance of <see cref="HtmlString"/>.</returns>
        public HtmlString GetHtml(HtmlHelper helper, string partial) {

            // Some input validation
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (string.IsNullOrWhiteSpace(partial)) throw new ArgumentNullException(nameof(partial));

            // Prepend the path to the "Rows" folder if not already specified
            if (GridUtils.IsValidPartialName(partial)) {
                partial = "TypedGrid/Rows/" + partial;
            }

            // Render the partial view
            return helper.Partial(partial, this);

        }

        /// <summary>
        /// Gets a textual representation of the row - eg. to be used in Examine.
        /// </summary>
        /// <returns>Returns an instance of <see cref="System.String"/> representing the value of the row.</returns>
        public override string GetSearchableText() {
            return Areas.Aggregate("", (current, area) => current + area.GetSearchableText());
        }

        #endregion

    }

}