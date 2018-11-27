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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class HtmlSnippets : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            AddColumn( "HtmlContent", "Name", c => c.String( maxLength: 100 ) );
            AlterColumn( "HtmlContent", "BlockId", c => c.Int( nullable: true ) );
            
            // Page: HtmlEditor Snippets Plugin Frame
            RockMigrationHelper.AddPage( true, "E7BD353C-91A6-4C15-A6C8-F44D0B16D16E", "2E169330-D7D7-4ECA-B417-72C64BE150F0", "HtmlEditor Snippets Plugin Frame", "", "93953B4B-AA72-4904-AB6F-B4E41AC758A6", "" ); // Site:Rock RMS
            RockMigrationHelper.AddPageRoute( "93953B4B-AA72-4904-AB6F-B4E41AC758A6", "htmleditorplugins/RockSnippets" );
            RockMigrationHelper.UpdateBlockType( "HtmlEditor Snippets", "Block to be used as part of the Snippets HtmlEditor Plugin", "~/Blocks/Utility/HtmlEditorSnippets.ascx", "Utility", "BE83B6D0-B1AD-4CCE-AF17-5603B50E6DF4" );
            // Add Block to Page: HtmlEditor Snippets Plugin Frame, Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "93953B4B-AA72-4904-AB6F-B4E41AC758A6", "", "BE83B6D0-B1AD-4CCE-AF17-5603B50E6DF4", "HtmlEditor Snippets", "Main", "", "", 0, "27280738-A3C5-495B-8F93-2948DC5217C8" );
            // Attrib Value for Block:HtmlEditor Snippets, Attribute:Goes By Page: HtmlEditor Snippets Plugin Frame, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "27280738-A3C5-495B-8F93-2948DC5217C8", "C31FDA8A-8CAB-4A1C-B96D-275415B5BB1C", @"william|robert|fred" );

        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropColumn( "HtmlContent", "Name" );
            AlterColumn( "HtmlContent", "BlockId", c => c.Int( nullable: false ) );

            RockMigrationHelper.DeleteBlock( "27280738-A3C5-495B-8F93-2948DC5217C8" );
            RockMigrationHelper.DeleteBlockType( "BE83B6D0-B1AD-4CCE-AF17-5603B50E6DF4" );
            RockMigrationHelper.DeletePage( "93953B4B-AA72-4904-AB6F-B4E41AC758A6" ); //  Page: HtmlEditor Snippets Plugin Frame
        }
    }
}
