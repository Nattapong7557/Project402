CREATE TABLE membership_payments (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL,
    member_name VARCHAR(100) NOT NULL,
    package VARCHAR(50) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    paid_at DATETIME(6) NOT NULL,
    CONSTRAINT membership_payment_user_unique UNIQUE (user_id),
    CONSTRAINT membership_payment_positive_amount CHECK (amount > 0),
    CONSTRAINT membership_payment_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX membership_payment_date (paid_at, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
