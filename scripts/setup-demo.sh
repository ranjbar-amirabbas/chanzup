#!/bin/bash

# Setup demo data for Chanzup application
echo "Setting up Chanzup demo environment..."

# Navigate to API directory
cd src/Chanzup.API

# Build the application
echo "Building the application..."
dotnet build

# Run database migrations and seed demo data
echo "Setting up database and seeding demo data..."
dotnet run --environment Development --seed-demo

echo "Demo setup complete!"
echo ""
echo "Demo Business Login Credentials:"
echo "Email: owner@coffeeshop.com"
echo "Password: DemoPassword123!"
echo ""
echo "Demo Player Login Credentials:"
echo "Email: demo@player.com"
echo "Password: PlayerPassword123!"
echo ""
echo "You can now:"
echo "1. Start the API: cd src/Chanzup.API && dotnet run"
echo "2. Start the React app: cd backoffice-app && npm start"
echo "3. Login to the business portal at http://localhost:3000/login"