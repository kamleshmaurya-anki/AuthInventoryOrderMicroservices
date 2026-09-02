-- ============================
-- Database: order_db (SQL Server)
-- ============================
IF DB_ID('order_db') IS NULL
BEGIN
    CREATE DATABASE order_db;
END
GO

USE order_db;
GO

IF OBJECT_ID('dbo.order_items', 'U') IS NOT NULL
    DROP TABLE dbo.order_items;
GO
IF OBJECT_ID('dbo.orders', 'U') IS NOT NULL
    DROP TABLE dbo.orders;
GO

-- ============================
-- Table: orders
-- ============================
CREATE TABLE dbo.orders (
    order_id       UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_orders_order_id DEFAULT NEWID(),
    user_id        UNIQUEIDENTIFIER NOT NULL,
    order_status   VARCHAR(30) NOT NULL,
    created_at     DATETIME2 NOT NULL CONSTRAINT DF_orders_created_at DEFAULT GETUTCDATE(),
    CONSTRAINT PK_orders PRIMARY KEY (order_id),
    CONSTRAINT chk_order_status CHECK (order_status IN ('CREATED', 'CONFIRMED', 'CANCELLED'))
);
GO

CREATE INDEX idx_orders_user_id ON dbo.orders(user_id);
GO

-- ============================
-- Table: order_items
-- ============================
CREATE TABLE dbo.order_items (
    order_item_id  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_order_items_id DEFAULT NEWID(),
    order_id       UNIQUEIDENTIFIER NOT NULL,
    product_id     UNIQUEIDENTIFIER NOT NULL,
    quantity       INT NOT NULL CONSTRAINT CHK_order_items_quantity CHECK (quantity > 0),
    CONSTRAINT PK_order_items PRIMARY KEY (order_item_id)
);
GO

-- ============================
-- Foreign Key (same service / same database only - order_items -> orders)
-- ============================
ALTER TABLE dbo.order_items
ADD CONSTRAINT fk_order_items_order
FOREIGN KEY (order_id)
REFERENCES dbo.orders(order_id)
ON DELETE CASCADE;
GO

CREATE INDEX idx_order_items_product_id ON dbo.order_items(product_id);
GO

-- Note: no foreign key from order_items.product_id to a products table.
-- Order Service intentionally does not share a database with Inventory
-- Service; product_id is only ever validated by calling Inventory Service's
-- API (see IInventoryServiceClient).
