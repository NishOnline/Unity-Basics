# Unity-Basics

Unity Movement.
This Script has basic unity movements with Jumping and Sprinting.
To use this script CORRECTLY:
1. Add a 3d Object like Cylinder name it Player
2. add the camera in child of Player Component.
3. Add the script in the Player Component and then drag the camera in the Player Camera Option.
4. This script also adds player force. Just apply rigidbody component to any mesh to move it with force.


To Display Fps:
1. Create an empty Canvas
2. create text mesh pro(GUI) to its Child and then Set its Text Content and its Position
3. Add Script to the Canvas and assing the textMeshPRo to the text option

To use Pause Menu Script:
1. Create A canvas
2. Add a Panel and Name it PauseMenu to its child
3. Now add your Buttons To its child and add text anddesign it to your Choice
4. Add The script to the canvas and the in the player field add your player Gameobejct which has the script PlayerMovement.cs
5. Its Almost done. In the buttons add onlick() functions and now add canvas object.
6. Beside this there would be a function. Select this and click on PauseMenu and find Resume().

To use EnemyFollow Script:
1. Go to Windows>AI>Navigation and set up its parameters.
2. Assign the script t whom you want to be Enemy.
3. Add NavMeshAgent to your Enemy
4. Add NavMeshSuface to the plane the enemy will Move and then make sure to click Bake.
5. Fill the Transforms in the EnemyFollow Script.

To use CarMovement: 
https://www.youtube.com/watch?v=LJr7satfAg8

To use PullDoor:
1. Add your Door in the scene and add a rigidbody component to its and make sure to Turn its "is Kinematic" on .
2. Add a empty gameobject and position it such that it becomes its hinge.
3. Name this DoorHinge and make sure that Door is its first child.
4. You can now check the working of your door by rotating the DoorHinge.
5. Attach the script to DoorHinge and drag the Player transform and camera(for this see playermovement script).

To use PickupAndThrow:
1. Attach the PickupAndThrow script to your player object.
2. Set up the crosshair
3. Make sure the items you want to be pickable have colliders and rigidbodies attached
4. Assign the layer of these items to a custom layer (e.g., "Pickable").
5. Set the pickupLayer field in the Inspector to this custom layer.
6. Create an empty GameObject as a child of the player where you want the picked-up items to be held (e.g., in front of the player). Assign this empty GameObject to the itemHolder field in the Inspector.
