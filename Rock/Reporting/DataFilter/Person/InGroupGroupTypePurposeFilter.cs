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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Reporting.DataFilter.Person
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Filter people on whether they are in a group of a specific group type purpose" )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Person In Group of Group Type Purpose Filter" )]
    public class InGroupGroupTypePurposeFilter : DataFilterComponent
    {
        #region Properties

        /// <summary>
        /// Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        /// The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Rock.Model.Person ).FullName; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Additional Filters"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "In Group of Group Type Purpose";
        }

        /// <summary>
        /// Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        /// will set the description of the filter to whatever is returned by this property.  If including script, the
        /// controls parent container can be referenced through a '$content' variable that is set by the control before 
        /// referencing this property.
        /// </summary>
        /// <value>
        /// The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return @"
function() {
  var groupTypePurposeName = $('.js-group-type-purpose', $content).find(':selected').text()
  var result = 'In group of group type with purpose: ' + groupTypePurposeName;

  var roleType = $('.js-member-type option:selected', $content).text();
  if (roleType && roleType != ""[All]"") {
     result = result + ', with role type: ' + roleType;
  }

  var groupStatus = $('.js-group-status option:selected', $content).text();
  if (groupStatus) {
     result = result + ', with group status: ' + groupStatus;
  }

  var groupMemberStatus = $('.js-group-member-status option:selected', $content).text();
  if (groupMemberStatus) {
     result = result + ', with member status: ' + groupMemberStatus;
  }

  return result;
}
";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            string result = "Group Member";
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                var groupType = GroupTypeCache.Get( selectionValues[0].AsGuid() );

                var groupTypeRoleGuidList = selectionValues[1].Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).Select( a => a.AsGuid() ).ToList();

                var groupTypeRoles = new GroupTypeRoleService( new RockContext() ).Queryable().Where( a => groupTypeRoleGuidList.Contains( a.Guid ) ).ToList();

                bool? groupStatus = null;
                if ( selectionValues.Length >= 4 )
                {
                    groupStatus = selectionValues[3].AsBooleanOrNull();
                }

                GroupMemberStatus? groupMemberStatus = null;
                if ( selectionValues.Length >= 3 )
                {
                    groupMemberStatus = selectionValues[2].ConvertToEnumOrNull<GroupMemberStatus>();
                }

                if ( groupType != null )
                {
                    result = string.Format( "In group of group type: {0}", groupType.Name );
                    if ( groupTypeRoles.Count() > 0 )
                    {
                        result += string.Format( ", with role(s): {0}", groupTypeRoles.Select( a => a.Name ).ToList().AsDelimited( "," ) );
                    }

                    if ( groupStatus.HasValue )
                    {
                        result += string.Format( ", with group status: {0}", groupStatus.Value ? "Active" : "Inactive" );
                    }

                    if ( groupMemberStatus.HasValue )
                    {
                        result += string.Format( ", with member status: {0}", groupMemberStatus.ConvertToString() );
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The Group Type Purpose Dropdown
        /// </summary>
        private RockDropDownList dllGroupTypePurpose = null;

        /// <summary>
        /// The Member Role Type Dropdown
        /// </summary>
        private RockDropDownList ddlMemberRoleType = null;

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            int? selectedPurposeId = null;
            if ( dllGroupTypePurpose != null )
            {
                selectedPurposeId = dllGroupTypePurpose.SelectedValueAsId();
            }

            dllGroupTypePurpose = new RockDropDownList();
            dllGroupTypePurpose.ID = filterControl.ID + "_groupTypePurposePicker";
            dllGroupTypePurpose.CssClass = "js-group-type-purpose";
            dllGroupTypePurpose.Label = "Group Type Purpose";
            dllGroupTypePurpose.BindToDefinedType( DefinedTypeCache.Get( SystemGuid.DefinedType.GROUPTYPE_PURPOSE ) );
            dllGroupTypePurpose.AutoPostBack = true;
            dllGroupTypePurpose.SelectedValue = selectedPurposeId.ToString();
            filterControl.Controls.Add( dllGroupTypePurpose );

            ddlMemberRoleType = new RockDropDownList();
            ddlMemberRoleType.Label = "with Role Type";
            ddlMemberRoleType.ID = filterControl.ID + "_ddlMemberRoleType";
            ddlMemberRoleType.CssClass = "js-member-type";
            ddlMemberRoleType.Items.Insert( 0, new ListItem( "[All]", "" ) );
            ddlMemberRoleType.Items.Insert( 1, new ListItem( "Leader", "True" ) );
            ddlMemberRoleType.Items.Insert( 2, new ListItem( "Non Leader", "False" ) );
            ddlMemberRoleType.SetValue( "" );
            filterControl.Controls.Add( ddlMemberRoleType );

            RockDropDownList ddlGroupMemberStatus = new RockDropDownList();
            ddlGroupMemberStatus.CssClass = "js-group-member-status";
            ddlGroupMemberStatus.ID = filterControl.ID + "_ddlGroupMemberStatus";
            ddlGroupMemberStatus.Label = "with Group Member Status";
            ddlGroupMemberStatus.Help = "Select a specific group member status to only include group members with that status. Leaving this blank will return all members.";
            ddlGroupMemberStatus.BindToEnum<GroupMemberStatus>( true );
            ddlGroupMemberStatus.SetValue( GroupMemberStatus.Active.ConvertToInt() );
            filterControl.Controls.Add( ddlGroupMemberStatus );

            RockDropDownList ddlGroupStatus = new RockDropDownList();
            ddlGroupStatus.CssClass = "js-group-status";
            ddlGroupStatus.ID = filterControl.ID + "_ddlGroupStatus";
            ddlGroupStatus.Label = "with Group Status";
            ddlGroupStatus.Items.Insert( 0, new ListItem( "[All]", "" ) );
            ddlGroupStatus.Items.Insert( 1, new ListItem( "Active", "True" ) );
            ddlGroupStatus.Items.Insert( 2, new ListItem( "Inactive", "False" ) );
            ddlGroupStatus.SetValue( true.ToString() );
            filterControl.Controls.Add( ddlGroupStatus );

            return new Control[4] { dllGroupTypePurpose, ddlMemberRoleType, ddlGroupMemberStatus, ddlGroupStatus };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls )
        {
            base.RenderControls( entityType, filterControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            var dllGroupTypePurpose = ( controls[0] as RockDropDownList );
            var ddlMemberRoleType = ( controls[1] as RockDropDownList );
            var ddlMemberStatus = ( controls[2] as RockDropDownList );
            var ddlGroupStatus = ( controls[3] as RockDropDownList );


            int groupTypePurposeId = dllGroupTypePurpose.SelectedValueAsId() ?? 0;

            var memberRoleType = ddlMemberRoleType.SelectedValue;

            var memberStatusValue = ddlMemberStatus.SelectedValue;

            var groupStatus = ddlGroupStatus.SelectedValue;

            return groupTypePurposeId.ToString() + "|" + memberRoleType + "|" + memberStatusValue + "|" + groupStatus;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                string groupTypePurposeId = selectionValues[0];
                ( controls[0] as RockDropDownList ).SelectedValue = groupTypePurposeId;

                string memberRoleType = selectionValues[1];
                ( controls[1] as RockDropDownList ).SelectedValue = memberRoleType;


                RockDropDownList ddlGroupMemberStatus = controls[2] as RockDropDownList;
                if ( selectionValues.Length >= 3 )
                {
                    ddlGroupMemberStatus.SetValue( selectionValues[2] );
                }
                else
                {
                    ddlGroupMemberStatus.SetValue( string.Empty );
                }

                RockDropDownList ddlGroupStatus = controls[3] as RockDropDownList;
                if ( selectionValues.Length >= 4 )
                {
                    ddlGroupStatus.SetValue( selectionValues[3] );
                }
                else
                {
                    ddlGroupStatus.SetValue( string.Empty );
                }
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                GroupTypeService groupTypeService = new GroupTypeService( ( RockContext ) serviceInstance.Context );
                GroupMemberService groupMemberService = new GroupMemberService( ( RockContext ) serviceInstance.Context );

                var groupTypePurposeId = selectionValues[0].AsInteger();
                var groupsQry = groupTypeService.Queryable().Where( gt => gt.GroupTypePurposeValueId == groupTypePurposeId )
                    .SelectMany( gt => gt.Groups );

                //Filter by group status
                bool? groupStatus = null;
                if ( selectionValues.Length >= 4 )
                {
                    groupStatus = selectionValues[3].AsBooleanOrNull();
                }

                if ( groupStatus.HasValue )
                {
                    groupsQry = groupsQry.Where( g => g.IsActive == groupStatus.Value );
                }

                var groupMemberQry = groupMemberService.Queryable().Where( gm => groupsQry.Select( g => g.Id ).Contains( gm.GroupId ) );

                //Filter by group status
                bool? groupRoleType = selectionValues[1].AsBooleanOrNull();
                if ( groupRoleType.HasValue )
                {
                    groupMemberQry = groupMemberQry.Where( gm => gm.GroupRole.IsLeader == groupRoleType.Value );
                }

                GroupMemberStatus? groupMemberStatus = null;
                if ( selectionValues.Length >= 3 )
                {
                    groupMemberStatus = selectionValues[2].ConvertToEnumOrNull<GroupMemberStatus>();
                }

                if ( groupMemberStatus.HasValue )
                {
                    groupMemberQry = groupMemberQry.Where( xx => xx.GroupMemberStatus == groupMemberStatus.Value );
                }

                var groupMemberIds = groupMemberQry.Select( gm => gm.PersonId )
                    .Distinct();

                var qry = new PersonService( ( RockContext ) serviceInstance.Context ).Queryable()
                    .Where( p => groupMemberIds.Contains( p.Id ) );

                Expression extractedFilterExpression = FilterExpressionExtractor.Extract<Rock.Model.Person>( qry, parameterExpression, "p" );

                return extractedFilterExpression;
            }

            return null;
        }

        #endregion
    }
}