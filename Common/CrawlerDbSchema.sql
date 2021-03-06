CREATE TABLE [dbo].[Uri](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AbsoluteUri] [nvarchar](max) NULL, --!!!! IMPORTANT: Set This Column As Case-Sensitive Collation !!!!
	[AbsolutePath] [nvarchar](max) NULL,
	[Host] [nvarchar](200) NULL,
	[Scheme] [nvarchar](50) NULL,
	[Fragment] [nvarchar](max) NULL,
	[Query] [nvarchar](max) NULL,
	[OriginalString] [nvarchar](max) NULL,
	[CreateAt] [datetime] NULL,
	[FailedAt] [datetime] NULL,
	[FailedException] [nvarchar](max) NULL,
	[CrawledAt] [datetime] NULL,
	[TimeTaken] [decimal](18, 8) NULL,
	[StatusCode] [int] NULL,
	[StatusCodeString] [nvarchar](50) NULL,
	[ContentLength] [int] NULL,
	[Content] [ntext] NULL,
	[Canonical] [nvarchar](max) NULL,
	[BrowserFailedAt] [datetime] NULL,
	[BrowserFailedException] [nvarchar](max) NULL,
	[BrowserCrawledAt] [datetime] NULL,
	[BrowserContent] [ntext] NULL,
 CONSTRAINT [PK_Uri] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO



CREATE TABLE [dbo].[Relation](
	[ParentId] [int] NOT NULL,
	[ChildId] [int] NOT NULL,
	[CreatedAt] [datetime] NULL,
	[IsBrowserRequired] [bit] NULL,
 CONSTRAINT [PK_Relation] PRIMARY KEY CLUSTERED 
(
	[ParentId] ASC,
	[ChildId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO



CREATE TABLE [dbo].[RedirectRelation](
	[SourceId] [int] NOT NULL,
	[DestinationId] [int] NOT NULL,
	[CreatedAt] [datetime] NULL,
 CONSTRAINT [PK_RedirectRelation] PRIMARY KEY CLUSTERED 
(
	[SourceId] ASC,
	[DestinationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO