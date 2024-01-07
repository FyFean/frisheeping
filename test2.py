import matplotlib.pyplot as plt

# Read data from file
file_path = "sheep_positions.txt"   # Replace with the actual file path
with open(file_path, "r") as file:
    lines = file.readlines()

# Extracting x and z coordinates for each sheep and each timestep
sheep_data = {}

for line in lines:
    coordinates = [float(coord) for coord in line.split(",")]

    # Iterate over each triplet (x, y, z)
    for i in range(0, len(coordinates), 3):
        sheep_id = i // 3  # Identify the sheep based on the triplet index
        x_coord = coordinates[i]
        z_coord = coordinates[i + 2]

        # Add data to the dictionary
        if sheep_id not in sheep_data:
            sheep_data[sheep_id] = {"x": [], "z": []}
        sheep_data[sheep_id]["x"].append(x_coord)
        sheep_data[sheep_id]["z"].append(z_coord)

# Plotting the 2D path for each sheep
for sheep_id, data in sheep_data.items():
    plt.plot(data["x"], data["z"], linestyle='-', label=f'Sheep {sheep_id}', color="gray", alpha=0.4)

plt.title('Sheep Movement Over Time')
plt.xlabel('X Coordinate')
plt.ylabel('Z Coordinate')
# plt.legend()
plt.grid(True)
plt.show()
