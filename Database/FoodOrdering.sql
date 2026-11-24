-- Install this feature once in the configured MySQL database, after the existing cafe schema.
CREATE TABLE food_menu_items (
    id INT AUTO_INCREMENT PRIMARY KEY,
    category VARCHAR(32) NOT NULL,
    name VARCHAR(150) NOT NULL UNIQUE,
    description VARCHAR(500) NOT NULL,
    image_url VARCHAR(500) NOT NULL,
    price DECIMAL(10,2) NOT NULL CHECK (price >= 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE food_orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL,
    request_token CHAR(32) NOT NULL,
    table_number INT NOT NULL CHECK (table_number BETWEEN 1 AND 99),
    status VARCHAR(24) NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,
    discount DECIMAL(10,2) NOT NULL,
    discount_code VARCHAR(32) NULL,
    total DECIMAL(10,2) NOT NULL CHECK (total >= 0),
    created_at DATETIME(6) NOT NULL,
    UNIQUE KEY food_order_request (user_id, request_token),
    KEY food_order_queue (status, id),
    CONSTRAINT food_order_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE food_order_items (
    id INT AUTO_INCREMENT PRIMARY KEY,
    order_id INT NOT NULL,
    menu_item_id INT NOT NULL,
    item_name VARCHAR(150) NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),
    quantity INT NOT NULL CHECK (quantity BETWEEN 1 AND 20),
    UNIQUE KEY food_order_menu (order_id, menu_item_id),
    CONSTRAINT food_item_order FOREIGN KEY (order_id) REFERENCES food_orders(id) ON DELETE CASCADE,
    CONSTRAINT food_item_menu FOREIGN KEY (menu_item_id) REFERENCES food_menu_items(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE food_payments (
    order_id INT PRIMARY KEY,
    recorded_by_user_id INT NULL,
    amount DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
    paid_at DATETIME(6) NOT NULL,
    KEY food_payment_date (paid_at),
    CONSTRAINT food_payment_order FOREIGN KEY (order_id) REFERENCES food_orders(id),
    CONSTRAINT food_payment_recorder FOREIGN KEY (recorded_by_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE food_customer_profiles (
    user_id INT PRIMARY KEY,
    birth_date DATE NOT NULL,
    CONSTRAINT food_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE food_birthday_redemptions (
    user_id INT NOT NULL,
    redemption_year INT NOT NULL,
    order_id INT NOT NULL UNIQUE,
    PRIMARY KEY (user_id, redemption_year),
    CONSTRAINT food_birthday_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT food_birthday_order FOREIGN KEY (order_id) REFERENCES food_orders(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE booking_charges (
    booking_id INT PRIMARY KEY,
    service_item_id INT NULL,
    subtotal DECIMAL(10,2) NOT NULL,
    discount DECIMAL(10,2) NOT NULL,
    total DECIMAL(10,2) NOT NULL CHECK (total >= 0),
    CONSTRAINT booking_charge_booking FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE CASCADE,
    CONSTRAINT booking_charge_service FOREIGN KEY (service_item_id) REFERENCES service_items(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO food_menu_items (category, name, description, image_url, price) VALUES
('coffee', 'Espresso', 'กาแฟช็อตเข้ม หอมกลิ่นกาแฟคั่ว เคลือบหน้าด้วยครีมา', 'https://images.unsplash.com/photo-1510591509098-f4fdc6d0ff04?auto=format&fit=crop&w=760&q=80', 65),
('coffee', 'Iced Cafe Latte', 'เอสเปรสโซกับนมเย็น เสิร์ฟบนน้ำแข็ง รสนุ่มละมุน', 'https://images.unsplash.com/photo-1461023058943-07fcbe16d735?auto=format&fit=crop&w=760&q=80', 85),
('coffee', 'Iced Americano', 'เอสเปรสโซเติมน้ำเย็น เสิร์ฟบนน้ำแข็ง รสกาแฟชัด', 'https://images.unsplash.com/photo-1517701604599-bb29b565090c?auto=format&fit=crop&w=760&q=80', 75),
('coffee', 'Cappuccino', 'เอสเปรสโซกับนมอุ่น ด้านบนเป็นฟองนมเนียนนุ่ม', 'https://images.unsplash.com/photo-1572442388796-11668a67e53d?auto=format&fit=crop&w=760&q=80', 85),
('tea', 'Uji Matcha Latte', 'มัทฉะอูจิหอมละมุน ผสมกับนมให้รสกลมกล่อม', 'https://images.unsplash.com/photo-1536256263959-770b48d82b0a?auto=format&fit=crop&w=760&q=80', 105),
('tea', 'Earl Grey Lemon', 'ชาเอิร์ลเกรย์หอมเบอร์กามอต ตัดด้วยรสเปรี้ยวของเลมอน', 'https://images.unsplash.com/photo-1556679343-c7306c1976bc?auto=format&fit=crop&w=760&q=80', 80),
('tea', 'Signature Thai Tea', 'ชาไทยกลิ่นหอม รสเข้มกลมกล่อม ดื่มเย็นสดชื่น', 'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=760&q=80', 75),
('beverage', 'Strawberry Soda', 'สตรอว์เบอร์รีรสเปรี้ยวหวาน เติมความซ่าด้วยโซดา', 'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=760&q=80', 80),
('beverage', 'Rich Dark Cocoa', 'โกโก้รสเข้ม หอมช็อกโกแลต และมีรสขมปลายเบา ๆ', 'https://images.unsplash.com/photo-1578314675249-a6910f80cc4e?auto=format&fit=crop&w=760&q=80', 95),
('beverage', 'Yuzu Honey Soda', 'ยูซุหอมเปรี้ยวกับน้ำผึ้ง เติมโซดาให้ซ่าสดชื่น', 'https://images.unsplash.com/photo-1621263764928-df1444c5e859?auto=format&fit=crop&w=760&q=80', 95),
('bread', 'Butter Croissant', 'ครัวซองต์ชั้นบางกรอบ ด้านในนุ่ม หอมเนยทุกคำ', 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=760&q=80', 85),
('bread', 'Shibuya Honey Toast', 'โทสต์แผ่นหนา กรอบนอกนุ่มใน หอมหวานจากน้ำผึ้ง', 'https://images.unsplash.com/photo-1484723091739-30a097e8f929?auto=format&fit=crop&w=760&q=80', 159),
('bread', 'Garlic Cheese Bread', 'ขนมปังอบหอมกระเทียม อร่อยกับชีสละลายด้านบน', 'https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=760&q=80', 95),
('cake', 'Strawberry Cake', 'เค้กเนื้อนุ่มกับสตรอว์เบอร์รี รสเปรี้ยวหวานกำลังดี', 'https://images.unsplash.com/photo-1565958011703-44f9829ba187?auto=format&fit=crop&w=760&q=80', 135),
('cake', 'Basque Cheesecake', 'ชีสเค้กหน้าไหม้หอมคาราเมล เนื้อด้านในนุ่มละมุน', 'https://images.unsplash.com/photo-1533134242443-d4fd215305ad?auto=format&fit=crop&w=760&q=80', 145),
('cake', 'Chocolate Fudge', 'เค้กช็อกโกแลตเนื้อแน่น หอมโกโก้และรสเข้มเต็มคำ', 'https://images.unsplash.com/photo-1606313564200-e75d5e30476c?auto=format&fit=crop&w=760&q=80', 139),
('donut', 'Original Glazed Donut', 'โดนัทเนื้อนุ่ม เคลือบน้ำตาลบาง ๆ ให้หวานพอดีคำ', 'https://images.unsplash.com/photo-1527515545081-5db817172677?auto=format&fit=crop&w=760&q=80', 49),
('donut', 'Chocolate Ring Donut', 'โดนัทวงแหวนเนื้อนุ่ม เคลือบหน้าด้วยช็อกโกแลต', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=760&q=80', 59),
('donut', 'Strawberry Sprinkles', 'โดนัทเคลือบรสสตรอว์เบอร์รี โรยเกล็ดน้ำตาลกรุบ ๆ', 'https://images.unsplash.com/photo-1551106652-a5bcf4b29ab6?auto=format&fit=crop&w=760&q=80', 59),
('donut', 'Matcha Cream Donut', 'โดนัทเนื้อนุ่มกับครีมมัทฉะ หอมชาเขียวละมุน', 'https://images.unsplash.com/photo-1583338917451-face2751d8d5?auto=format&fit=crop&w=760&q=80', 69),
('western', 'Spaghetti Carbonara', 'สปาเกตตีคลุกซอสคาร์โบนารา รสเค็มมันกลมกล่อม', 'https://images.unsplash.com/photo-1612874742237-6526221588e3?auto=format&fit=crop&w=760&q=80', 189),
('western', 'Salmon Steak', 'สเต๊กแซลมอนย่างหอม เนื้อฉ่ำและรสปลาชัด', 'https://images.unsplash.com/photo-1467003909585-2f8a72700288?auto=format&fit=crop&w=760&q=80', 259),
('western', 'Angus Beef Burger', 'เบอร์เกอร์เนื้อแองกัสย่างฉ่ำ ประกบด้วยขนมปังนุ่ม', 'https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=760&q=80', 239),
('western', 'Caesar Salad', 'สลัดผักกรอบคลุกเดรสซิงซีซาร์ รสเค็มมันกลมกล่อม', 'https://images.unsplash.com/photo-1546793665-c74683f339c1?auto=format&fit=crop&w=760&q=80', 159);
