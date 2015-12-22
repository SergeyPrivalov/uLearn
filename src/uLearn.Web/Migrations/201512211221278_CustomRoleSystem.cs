using uLearn.Web.Models;

namespace uLearn.Web.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class CustomRoleSystem : DbMigration
	{
		public override void Up()
		{
			CreateTable(
				"dbo.UserRoles",
				c => new
				{
					Id = c.Int(nullable: false, identity: true),
					UserId = c.String(maxLength: 128),
					CourseId = c.String(nullable: false),
					Role = c.Int(nullable: false),
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.AspNetUsers", t => t.UserId)
				.Index(t => t.UserId);
			Sql(string.Format(@"
					declare @adminId nvarchar(max);
					set @adminId = (select id from AspNetRoles where Name = '{0}');
					insert 
						into AspNetUserRoles(UserId, RoleId)
						(
							select distinct UserId, @adminId 
								from AspNetUserRoles as t 
								where not exists (
									select 1 
									from AspNetUserRoles as d 
									where t.UserId = d.UserId and d.RoleId = @adminId
								)
						);",
				LmsRoles.Admin)
				);

			Sql(string.Format(@"delete from AspNetRoles where Name != '{0}';", LmsRoles.Admin));
		}

		public override void Down()
		{
			DropForeignKey("dbo.UserRoles", "UserId", "dbo.AspNetUsers");
			DropIndex("dbo.UserRoles", new[] { "UserId" });
			DropTable("dbo.UserRoles");
		}
	}
}
