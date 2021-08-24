# FlexibleGridLayoutUnity
An upgraded version of GridLayoutGroup for Unity

I took the FlexibleGridLayout script from Game Dev Guide(Youtuber) and upgraded it
Here is the link for [Game Dev Guide Script](https://www.youtube.com/watch?v=CGsEJToeXmA)

FlexibleGridLayout is a grid layout that supports only same size cell
FlexibleScrollableGridLayout is subclass of FlexibleGridLayout that supports scrollability, No need to use ContentSizeFitter
MonsterGridLayout is a grid layout that supports same cell size and custom cell size with more features
TBH FlexibleGridLayout will suffice for 99% of usage

To use FlexibleScrollableGridLayout you have to wrap the object that hold the FlexibleScrollableGridLayout 
And give the wrapper object the real width and height you want to use for the grid layout
For example when using the default Scroll View component the FlexibleScrollableGridLayout needs to be on the "content" game object

If you don't like my custom editor in FlexibleGridLayout just remove the custom editor and remove the [HideInInspector] from all the parameters
If you remove the custom editor from MonsterGridLayout you are on your own :D

If you want to change the logic for MonsterGridLayout you can inherit from it and change the calculations for:
1. The number of rows and columns
2. Row/Column position, By that you can determine how the grid will populate the rows/columns(in what pattern)
3. xPosForCellsByChildAlignment/yPosForCellsByChildAlignment, Probably you wont need to change it, But just in case you want/need

Download the script do whatever you want with it, And of course ENJOY!
