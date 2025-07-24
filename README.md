PPG Background Jobs Micro-Services
===========

Overview
--------
This is the code for the Background Jobs micro-services of Crown Commercial Service's (_CCS_)
Public Procurement Gateway (_PPG_).

Technology Overview
---------
The project is implemented as a .NET Core web application, implemented using NuGet.

The core technologies for the project are:

* .NET Core 9
* NuGet for package management
* XUnit for unit testing

Building and Running Locally
----------------------------
This repository holds six runnable applications:
* CcsSso.Core.ReportingScheduler
* CcsSso.Core.PPONScheduler
* CcsSso.Core.JobScheduler
* CcsSso.Core.DormancyJobScheduler
* CcsSso.Core.DelegationJobScheduler
* CcsSso.Core.DataMigrationJobScheduler

To run the applications locally, you simply need to run any of the above listed projects.

For each of these projects, you will need to be supplied with a local secrets file (`appsecrets.json`) to enable the project to run, which can be supplied by any member of the development team.

You will also need to have setup 6 PostgreSQL databases locally, which the application can run against. To make this easier, database dumps can be supplied by any member of the development team which you can restore to setup and populate these databases.

As these applications are background jobs, they are not directly accessible once running.

Branches
--------
When picking up tickets, branches should be created using the **_feature/*_** format.

When completed, these branches should be pull requested against _**develop**_ for review and approval.  _**develop**_ is then built out onto the **Development** environment.

The **Test** and **Pre-Production** environments are controlled via means of release and hotfix branches.

Release branches should be created from _**develop**_ using the **_release/*_** format, whilst hotfixes should be created from _**main**_ using the **_hotfix/*_** format.  These branches can then be built out to **Test** and **Pre-Production** as appropriate.

When releases/hotfixes are ready for deployment to **Production**, the **_release/*_** or **_hotfix/*_** branch in question should be pull requested against the _**main**_ branch for review and approval.  This branch should then be built out to **Production**.

Once a release/hotfix has been completed you should be sure to merge _**main**_ back down into _**develop**_.
