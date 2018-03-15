namespace Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAntiPlagiarismSubmissionId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserExerciseSubmissions", "AntiPlagiarismSubmissionId", c => c.Int());
            CreateIndex("dbo.UserExerciseSubmissions", "AntiPlagiarismSubmissionId", name: "IDX_UserExerciseSubmission_ByAntiPlagiarismSubmissionId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.UserExerciseSubmissions", "IDX_UserExerciseSubmission_ByAntiPlagiarismSubmissionId");
            DropColumn("dbo.UserExerciseSubmissions", "AntiPlagiarismSubmissionId");
        }
    }
}
