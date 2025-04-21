CREATE DATABASE postgres;

-- Connect to Database
\c postgres;

CREATE TABLE "JobTitles" (
                             "Id" SERIAL PRIMARY KEY,
                             "Name" VARCHAR(255) NOT NULL
);

CREATE TABLE "Departments" (
                               "Id" SERIAL PRIMARY KEY,
                               "Name" VARCHAR(255) NOT NULL,
                               "ParentId" INTEGER NULL,
                               "ManagerId" INTEGER NULL,
                               "Phone" VARCHAR(50) NULL,
                               CONSTRAINT "FK_Departments_Departments_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Employees" (
                             "Id" SERIAL PRIMARY KEY,
                             "FullName" VARCHAR(255) NOT NULL,
                             "Login" VARCHAR(50) NOT NULL,
                             "Password" VARCHAR(255) NOT NULL,
                             "DepartmentId" INTEGER NULL,
                             "JobTitleId" INTEGER NULL,
                             CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL,
                             CONSTRAINT "FK_Employees_JobTitles_JobTitleId" FOREIGN KEY ("JobTitleId") REFERENCES "JobTitles" ("Id") ON DELETE SET NULL
);

-- Add managers foreign key to get managers from employees
ALTER TABLE "Departments" ADD CONSTRAINT "FK_Departments_Employees_ManagerId"
    FOREIGN KEY ("ManagerId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;

-- Add Indexes to columns with search operations (optimization)
CREATE INDEX "IX_Departments_Name" ON "Departments" ("Name");
CREATE INDEX "IX_Departments_ParentId" ON "Departments" ("ParentId");
CREATE INDEX "IX_Departments_ManagerId" ON "Departments" ("ManagerId");
CREATE INDEX "IX_Employees_FullName" ON "Employees" ("FullName");
CREATE INDEX "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");
CREATE INDEX "IX_Employees_JobTitleId" ON "Employees" ("JobTitleId");
CREATE INDEX "IX_JobTitles_Name" ON "JobTitles" ("Name");