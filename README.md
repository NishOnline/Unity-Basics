# Unity-Basics

Unity Movement
This Script has basic unity movements with Jumping and Sprinting.
To use this script CORRECTLY:
1. Add a 3d Object like Cylinder name it Player
2. add the camera in child of Player Component.
3. Add the script in the Player Component and then drag the camera in the Player Camera Option.


To Display Fps:
1. Create an empty Canvas
2. create text mesh pro to its Child and then Set its Text Content and its Position
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
