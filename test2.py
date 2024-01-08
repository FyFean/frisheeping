import matplotlib.pyplot as plt
import numpy as np
# Read data from file
file_path = "sheep_positions.txt"   # Replace with the actual file path
file_path2 = "dog_positions.txt"   # Replace with the actual file path

with open(file_path, "r") as file:
    lines = file.readlines()

with open(file_path2, "r") as file:
    lines2 = file.readlines()

# Extracting x and z coordinates for each sheep and each timestep
sheep_data = {}
dog_data = {}


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

# for line in lines2:
#     coordinates = [float(coord) for coord in line.split(",")]

#     # Iterate over each triplet (x, y, z)
#     for i in range(0, len(coordinates), 3):
#         dog_id = i // 3  # Identify the sheep based on the triplet index
#         x_coord = coordinates[i]
#         z_coord = coordinates[i + 2]

#         # Add data to the dictionary
#         if dog_id not in dog_data:
#             dog_data[dog_id] = {"x": [], "z": []}
#         dog_data[dog_id]["x"].append(x_coord)
#         dog_data[dog_id]["z"].append(z_coord)

dog_x = [float(coord.split(",")[0]) for coord in lines2]
dog_z = [float(coord.split(",")[2]) for coord in lines2]

# speed significance heading - boids, dog repulsion ni fuzzyfied
# theta velocity
plt.figure(figsize=(10, 8))

for sheep_id, data in sheep_data.items():
    plt.plot(data["x"], data["z"], linestyle='-', color="gray", alpha=0.4, label='Sheep' if sheep_id == 1 else None)

# Plotting the 2D path for the dog
plt.plot(dog_x, dog_z, linestyle='-', color="red", alpha=1, linewidth=1.2, label='Dog')

# Legend for both sheep and dog
plt.legend(loc='upper right', title='Legend')

plt.title('Strombom Fuzzy Model Sheep and Dog Movement Over Time')
plt.xlabel('X Coordinate')
plt.ylabel('Z Coordinate')
# plt.legend()
plt.grid(True)
plt.show()
