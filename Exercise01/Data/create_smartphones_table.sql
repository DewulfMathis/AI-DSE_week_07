-- Table for Exercise01.Models.Smartphone
CREATE TABLE IF NOT EXISTS smartphones
(
    id               INT           NOT NULL AUTO_INCREMENT,
    brand            VARCHAR(50)   NOT NULL,
    type             VARCHAR(100)  NOT NULL,
    release_year     SMALLINT      NOT NULL,
    start_price      DECIMAL(10,2) NOT NULL,
    operating_system VARCHAR(50)   NOT NULL,
    PRIMARY KEY (id)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;
