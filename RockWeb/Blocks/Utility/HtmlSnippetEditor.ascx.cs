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
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.Utility
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Html Snippet Editor" )]
    [Category( "Utility" )]
    [Description( "Block to edit HTML Snippets" )]
    public partial class HtmlSnippetEditor : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !this.IsPostBack )
            {
                var id = PageParameter( "htmlcontentid" ).AsInteger();
                var rockContext = new RockContext();
                HtmlContentService htmlContentService = new HtmlContentService( rockContext );
                var htmlContent = htmlContentService.Get( id );
                if ( htmlContent != null )
                {
                    if ((htmlContent.CreatedByPersonAlias != null && htmlContent.CreatedByPersonAlias.PersonId == CurrentPersonId)
                        || htmlContent.IsAuthorized(Authorization.EDIT, CurrentPerson))
                    {
                        tbName.Text = htmlContent.Name;
                        heSnippet.Text = htmlContent.Content;
                        rockContext.SaveChanges();
                    }
                    else
                    {
                        upSnippets.Visible = false;
                        nbMessage.Text = "You are not authorized to edit this snippet.";
                        nbMessage.Visible = true;
                    }
                } 
            }

        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            var id = PageParameter( "htmlcontentid" ).AsInteger();
            var rockContext = new RockContext();
            HtmlContentService htmlContentService = new HtmlContentService( rockContext );
            var htmlContent = htmlContentService.Get( id );
            if (htmlContent == null)
            {
                htmlContent = new HtmlContent();
                htmlContentService.Add( htmlContent );
                htmlContent.CreatedByPersonAliasId = CurrentPersonAliasId;

            } else if ( !(( htmlContent.CreatedByPersonAlias != null && htmlContent.CreatedByPersonAlias.PersonId == CurrentPersonId )
                || htmlContent.IsAuthorized( Authorization.EDIT, CurrentPerson )) )
            {
                upSnippets.Visible = false;
                nbMessage.Text = "You are not authorized to edit this snippet.";
                nbMessage.Visible = true;
                return;
            }
            htmlContent.Name = tbName.Text;
            htmlContent.Content = heSnippet.Text;

            rockContext.SaveChanges();
            NavigateToParentPage();
        }
    }
}