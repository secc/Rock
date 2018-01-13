// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.Utility
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "HtmlEditor Snippets" )]
    [Category( "Utility" )]
    [Description( "Block to be used as part of the Snippets HtmlEditor Plugin" )]
    public partial class HtmlEditorSnippets : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
           
            if ( !this.IsPostBack )
            {

                pnlModalHeader.Visible = PageParameter( "ModalMode" ).AsBoolean();
                pnlModalFooterActions.Visible = PageParameter( "ModalMode" ).AsBoolean();
                lTitle.Text = PageParameter( "Title" );
                BindGrid();
            }
            
        }

        protected void BindGrid()
        {
            HtmlContentService htmlContentService = new HtmlContentService( new RockContext() );


            List<int> personAliasIds = CurrentPerson.Aliases.Select( a => a.Id ).ToList();
            var query = htmlContentService.Queryable()
                .Where( hc => hc.BlockId == null &&  hc.CreatedByPersonAliasId.HasValue
                        && personAliasIds.Contains( hc.CreatedByPersonAliasId.Value ) );
            gSnippets.DataSource = query.ToList();
            gSnippets.DataBind();
        }

        #endregion

    }
}