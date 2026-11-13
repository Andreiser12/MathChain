BEGIN;

DROP VIEW "Assignmentss";
DROP TABLE "MessageComments";

ALTER TABLE "Assignments" RENAME CONSTRAINT "PK_Assignmentss" TO "PK_Assignments";
ALTER TABLE "ClassAnnouncements" RENAME CONSTRAINT "ClassAnnouncements_pkey" TO "PK_ClassAnnouncements";
ALTER TABLE "ClassMaterials" RENAME CONSTRAINT "ClassMaterials_pkey" TO "PK_ClassMaterials";
ALTER TABLE "ClassMedia" RENAME CONSTRAINT "ClassMedia_pkey" TO "PK_ClassMedia";
ALTER TABLE "CurriculumFormulas" RENAME CONSTRAINT "CurriculumFormulas_pkey" TO "PK_CurriculumFormulas";
ALTER TABLE "CurriculumFormulas" RENAME CONSTRAINT "CurriculumFormulas_ChapterId_fkey" TO "FK_CurriculumFormulas_CurriculumChapters_ChapterId";

ALTER TABLE "ClassAnnouncements" ALTER COLUMN "Id" DROP DEFAULT, ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "ClassMedia" ALTER COLUMN "Id" DROP DEFAULT, ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "CurriculumFormulas" ALTER COLUMN "CreatedAt" DROP DEFAULT, ALTER COLUMN "UpdatedAt" DROP DEFAULT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261113085956_SyncCurriculumSchema', '8.0.32');

ALTER TABLE "Users" ALTER COLUMN "WalletAddress" TYPE character varying(100);

ALTER TABLE "Submissions" ALTER COLUMN "StudentWallet" TYPE character varying(100);

ALTER TABLE "Enrollments" ALTER COLUMN "StudentWallet" TYPE character varying(100);

ALTER TABLE "CurriculumFormulas" ALTER COLUMN "Requirement" TYPE character varying(1000);

ALTER TABLE "CurriculumFormulas" ALTER COLUMN "Description" TYPE character varying(2000);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "Version" SET DEFAULT 1;

ALTER TABLE "CurriculumChapters" ALTER COLUMN "Title" TYPE character varying(200);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "IpfsHash" TYPE character varying(100);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "ImageUrl" TYPE character varying(500);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "Description" TYPE character varying(1000);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "ChapterSlug" TYPE character varying(150);

ALTER TABLE "CurriculumChapters" ALTER COLUMN "Category" TYPE character varying(100);
UPDATE "CurriculumChapters" SET "Category" = '' WHERE "Category" IS NULL;
ALTER TABLE "CurriculumChapters" ALTER COLUMN "Category" SET NOT NULL;
ALTER TABLE "CurriculumChapters" ALTER COLUMN "Category" SET DEFAULT '';

ALTER TABLE "ClassRooms" ALTER COLUMN "TeacherWallet" TYPE character varying(100);

ALTER TABLE "ClassRooms" ALTER COLUMN "Name" TYPE character varying(150);

ALTER TABLE "ClassRooms" ALTER COLUMN "JoinCode" TYPE character varying(20);

ALTER TABLE "Assignments" ALTER COLUMN "Title" TYPE character varying(200);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261113090002_AlignColumnLengths', '8.0.32');


COMMIT;
