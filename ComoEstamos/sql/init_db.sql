USE [test_fin]
GO
/****** Objeto:  Table [dbo].[tb_carteira]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_carteira](
	[id_carteira] [int] IDENTITY(1,1) NOT NULL,
	[id_usuario] [int] NOT NULL,
	[nm_carteira] [nvarchar](50) NOT NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_carteira] PRIMARY KEY CLUSTERED 
(
	[id_carteira] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_categoria]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_categoria](
	[id_categoria] [int] IDENTITY(1,1) NOT NULL,
	[nm_categoria] [nvarchar](100) NOT NULL,
	[img_categoria] [varbinary](max) NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_categoria] PRIMARY KEY CLUSTERED 
(
	[id_categoria] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_conta]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_conta](
	[id_conta] [int] IDENTITY(1,1) NOT NULL,
	[id_usuario] [int] NOT NULL,
	[nm_conta] [nvarchar](100) NOT NULL,
	[img_conta] [varbinary](max) NULL,
	[nr_saldo] [numeric](10, 2) NOT NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_conta] PRIMARY KEY CLUSTERED 
(
	[id_conta] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_credor]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_credor](
	[id_credor] [int] IDENTITY(1,1) NOT NULL,
	[nm_credor] [nvarchar](50) NOT NULL,
	[ds_observacoes] [nvarchar](max) NULL,
	[img_logo] [varbinary](max) NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_credor] PRIMARY KEY CLUSTERED 
(
	[id_credor] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_divida]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_divida](
	[id_divida] [int] IDENTITY(1,1) NOT NULL,
	[id_usuario] [int] NOT NULL,
	[id_credor] [int] NULL,
	[nm_divida] [nvarchar](100) NOT NULL,
	[dia_vencimento] [int] NOT NULL,
	[dt_primeiro_vencimento] [datetime] NOT NULL,
	[nr_parcelas] [int] NOT NULL,
	[nr_valor] [numeric](10, 2) NOT NULL,
	[dm_ativo] [bit] NOT NULL,
	[id_conta] [int] NULL,
	[id_categoria] [int] NULL,
	[dm_divida] [bit] NULL,
 CONSTRAINT [PK_tb_divida] PRIMARY KEY CLUSTERED 
(
	[id_divida] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_historico]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_historico](
	[id_historico] [int] IDENTITY(1,1) NOT NULL,
	[dt_historico] [datetime] NOT NULL,
	[id_investimento] [int] NOT NULL,
	[nm_investimento] [nvarchar](50) NOT NULL,
	[nr_quantidade] [numeric](10, 2) NOT NULL,
	[vl_cotacao] [numeric](10, 2) NOT NULL,
	[ds_observacao] [nvarchar](500) NOT NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_historico] PRIMARY KEY CLUSTERED 
(
	[id_historico] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_investimento]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_investimento](
	[id_investimento] [int] IDENTITY(1,1) NOT NULL,
	[id_carteira] [int] NOT NULL,
	[nm_investimento] [nvarchar](50) NOT NULL,
	[nr_quantidade] [numeric](10, 2) NOT NULL,
	[vl_cotacao] [numeric](10, 2) NOT NULL,
	[ds_observacao] [nvarchar](500) NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_investimento] PRIMARY KEY CLUSTERED 
(
	[id_investimento] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_operacao]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_operacao](
	[id_operacao] [int] IDENTITY(1,1) NOT NULL,
	[id_investimento] [int] NOT NULL,
	[dm_compra] [bit] NOT NULL,
	[dt_operacao] [datetime] NOT NULL,
	[nr_quantidade] [int] NOT NULL,
	[vl_operacao] [numeric](10, 2) NOT NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_operacao] PRIMARY KEY CLUSTERED 
(
	[id_operacao] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_parcela]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_parcela](
	[id_parcela] [int] IDENTITY(1,1) NOT NULL,
	[id_divida] [int] NOT NULL,
	[id_categoria] [int] NOT NULL,
	[id_conta] [int] NOT NULL,
	[ds_parcela] [nvarchar](100) NOT NULL,
	[nr_valor] [numeric](10, 2) NOT NULL,
	[dt_vencimento] [datetime] NOT NULL,
	[dt_pagamento] [datetime] NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_parcela] PRIMARY KEY CLUSTERED 
(
	[id_parcela] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto:  Table [dbo].[tb_usuario]    Data do Script: 23/09/2026 16:51:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tb_usuario](
	[id_usuario] [int] IDENTITY(1,1) NOT NULL,
	[nm_usuario] [nvarchar](100) NOT NULL,
	[ds_email] [nvarchar](100) NOT NULL,
	[ds_senha] [nvarchar](100) NOT NULL,
	[img_usuario] [varbinary](max) NULL,
	[dm_ativo] [bit] NOT NULL,
 CONSTRAINT [PK_tb_usuario] PRIMARY KEY CLUSTERED 
(
	[id_usuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_tb_usuario_ds_email] UNIQUE NONCLUSTERED 
(
	[ds_email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[tb_carteira] ADD  CONSTRAINT [DF_tb_carteira_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_categoria] ADD  CONSTRAINT [DF_tb_categoria_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_conta] ADD  CONSTRAINT [DF_tb_conta_nr_saldo]  DEFAULT ((0)) FOR [nr_saldo]
GO
ALTER TABLE [dbo].[tb_conta] ADD  CONSTRAINT [DF_tb_conta_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_credor] ADD  CONSTRAINT [DF_tb_credor_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_divida] ADD  CONSTRAINT [DF_tb_divida_dia_vencimento]  DEFAULT ((10)) FOR [dia_vencimento]
GO
ALTER TABLE [dbo].[tb_divida] ADD  CONSTRAINT [DF_tb_divida_nr_parcelas]  DEFAULT ((1)) FOR [nr_parcelas]
GO
ALTER TABLE [dbo].[tb_divida] ADD  CONSTRAINT [DF_tb_divida_nr_valor]  DEFAULT ((0)) FOR [nr_valor]
GO
ALTER TABLE [dbo].[tb_divida] ADD  CONSTRAINT [DF_tb_divida_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_divida] ADD  CONSTRAINT [DF_tb_divida_dm_divida]  DEFAULT ((1)) FOR [dm_divida]
GO
ALTER TABLE [dbo].[tb_historico] ADD  CONSTRAINT [DF_tb_historico_dt_historico]  DEFAULT (getdate()) FOR [dt_historico]
GO
ALTER TABLE [dbo].[tb_investimento] ADD  CONSTRAINT [DF_tb_investimento_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_operacao] ADD  CONSTRAINT [DF_tb_operacao_dt_operacao]  DEFAULT (getdate()) FOR [dt_operacao]
GO
ALTER TABLE [dbo].[tb_operacao] ADD  CONSTRAINT [DF_tb_operacao_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_parcela] ADD  CONSTRAINT [DF_tb_parcela_nr_valor]  DEFAULT ((0)) FOR [nr_valor]
GO
ALTER TABLE [dbo].[tb_parcela] ADD  CONSTRAINT [DF_tb_parcela_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_usuario] ADD  CONSTRAINT [DF_tb_usuario_dm_ativo]  DEFAULT ((1)) FOR [dm_ativo]
GO
ALTER TABLE [dbo].[tb_carteira]  WITH CHECK ADD  CONSTRAINT [FK_tb_carteira_tb_usuario] FOREIGN KEY([id_usuario])
REFERENCES [dbo].[tb_usuario] ([id_usuario])
GO
ALTER TABLE [dbo].[tb_carteira] CHECK CONSTRAINT [FK_tb_carteira_tb_usuario]
GO
ALTER TABLE [dbo].[tb_conta]  WITH CHECK ADD  CONSTRAINT [FK_tb_conta_tb_usuario] FOREIGN KEY([id_usuario])
REFERENCES [dbo].[tb_usuario] ([id_usuario])
GO
ALTER TABLE [dbo].[tb_conta] CHECK CONSTRAINT [FK_tb_conta_tb_usuario]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [FK_tb_divida_tb_categoria] FOREIGN KEY([id_categoria])
REFERENCES [dbo].[tb_categoria] ([id_categoria])
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [FK_tb_divida_tb_categoria]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [FK_tb_divida_tb_conta] FOREIGN KEY([id_conta])
REFERENCES [dbo].[tb_conta] ([id_conta])
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [FK_tb_divida_tb_conta]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [FK_tb_divida_tb_credor] FOREIGN KEY([id_credor])
REFERENCES [dbo].[tb_credor] ([id_credor])
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [FK_tb_divida_tb_credor]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [FK_tb_divida_tb_usuario] FOREIGN KEY([id_usuario])
REFERENCES [dbo].[tb_usuario] ([id_usuario])
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [FK_tb_divida_tb_usuario]
GO
ALTER TABLE [dbo].[tb_historico]  WITH CHECK ADD  CONSTRAINT [FK_tb_historico_tb_investimento] FOREIGN KEY([id_investimento])
REFERENCES [dbo].[tb_investimento] ([id_investimento])
GO
ALTER TABLE [dbo].[tb_historico] CHECK CONSTRAINT [FK_tb_historico_tb_investimento]
GO
ALTER TABLE [dbo].[tb_investimento]  WITH CHECK ADD  CONSTRAINT [FK_tb_investimento_tb_carteira] FOREIGN KEY([id_carteira])
REFERENCES [dbo].[tb_carteira] ([id_carteira])
GO
ALTER TABLE [dbo].[tb_investimento] CHECK CONSTRAINT [FK_tb_investimento_tb_carteira]
GO
ALTER TABLE [dbo].[tb_operacao]  WITH CHECK ADD  CONSTRAINT [FK_tb_operacao_tb_investimento] FOREIGN KEY([id_investimento])
REFERENCES [dbo].[tb_investimento] ([id_investimento])
GO
ALTER TABLE [dbo].[tb_operacao] CHECK CONSTRAINT [FK_tb_operacao_tb_investimento]
GO
ALTER TABLE [dbo].[tb_parcela]  WITH CHECK ADD  CONSTRAINT [FK_tb_parcela_tb_categoria] FOREIGN KEY([id_categoria])
REFERENCES [dbo].[tb_categoria] ([id_categoria])
GO
ALTER TABLE [dbo].[tb_parcela] CHECK CONSTRAINT [FK_tb_parcela_tb_categoria]
GO
ALTER TABLE [dbo].[tb_parcela]  WITH CHECK ADD  CONSTRAINT [FK_tb_parcela_tb_conta] FOREIGN KEY([id_conta])
REFERENCES [dbo].[tb_conta] ([id_conta])
GO
ALTER TABLE [dbo].[tb_parcela] CHECK CONSTRAINT [FK_tb_parcela_tb_conta]
GO
ALTER TABLE [dbo].[tb_parcela]  WITH CHECK ADD  CONSTRAINT [FK_tb_parcela_tb_divida] FOREIGN KEY([id_divida])
REFERENCES [dbo].[tb_divida] ([id_divida])
GO
ALTER TABLE [dbo].[tb_parcela] CHECK CONSTRAINT [FK_tb_parcela_tb_divida]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [CK_tb_divida_dia_vencimento] CHECK  (([dia_vencimento]>=(1) AND [dia_vencimento]<=(31)))
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [CK_tb_divida_dia_vencimento]
GO
ALTER TABLE [dbo].[tb_divida]  WITH CHECK ADD  CONSTRAINT [CK_tb_divida_nr_valor] CHECK  (([nr_valor]>=(0)))
GO
ALTER TABLE [dbo].[tb_divida] CHECK CONSTRAINT [CK_tb_divida_nr_valor]
GO
