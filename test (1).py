import numpy as np
import matplotlib.pyplot as plt

def triangular_membership(x, a, b, c):
    if x <= a or x > c:
        return 0.0
    elif a < x <= b:
        return (x - a) / (b - a)
    elif b < x <= c:
        return (c - x) / (c - b)
    return 0.0

def sigmoidal_membership(x, c, alpha):
    return 1.0 / (1.0 + np.exp(-alpha * (x - c)))

def trapezoidal_membership(x, a, b, c, d):
    if x <= a or x > d:
        return 0.0
    elif a < x <= b:
        return (x - a) / (b - a)
    elif b < x <= c:
        return 1.0
    elif c < x <= d:
        return (d - x) / (d - c)
    return 0.0

def plot_membership_function(ax, membership_function, params, label, color):
    x_values = np.linspace(0, 100, 1000)
    y_values = [membership_function(x, *params) for x in x_values]
    ax.plot(x_values, y_values, color=color)
    ax.set_title(label)
    ax.set_xlabel("Value")
    ax.set_ylabel("Membership")

# Positive and negative pairs for each characteristic
characteristics = ["Adventurous", "Agreeableness", "Extraversion", "DogRepulsion"]
# Create subplots for each positive-negative pair
fig, axs = plt.subplots(len(characteristics))

# Plot each positive-negative pair on its own subplot
for i, characteristic in enumerate(characteristics):
    # label = f"{characteristic} - {pair[0]}"
    pos_color = "green"
    neg_color = "red"
    neutral_color = "blue"

    if characteristic == "Adventurous":
        plot_membership_function(
            axs[i], triangular_membership, (0, 40, 80), characteristic, pos_color)
        plot_membership_function(
            axs[i], triangular_membership, (20, 60, 80), characteristic, neg_color)
        plot_membership_function(
            axs[i],triangular_membership,( 20, 80, 100), characteristic, neutral_color)
    elif characteristic == "Agreeableness":
        plot_membership_function(
            axs[i], sigmoidal_membership, (10, 10), characteristic, pos_color)
        plot_membership_function(
            axs[i], sigmoidal_membership, (30, 1), characteristic, neg_color)
    elif characteristic == "Extraversion":
        plot_membership_function(
            axs[i], triangular_membership, (0, 20, 40), characteristic, pos_color)
        plot_membership_function(
            axs[i], triangular_membership, (10, 60, 100), characteristic, neg_color)
    elif characteristic == "DogRepulsion":
        plot_membership_function(
            axs[i], trapezoidal_membership, ( 0, 20, 40, 60), characteristic, pos_color)
        plot_membership_function(
            axs[i], trapezoidal_membership, ( 40, 60, 80, 100), characteristic, neg_color)
        plot_membership_function(
            axs[i], trapezoidal_membership, ( 20,40,60,80), characteristic, neutral_color)
            

plt.tight_layout()
plt.show()
