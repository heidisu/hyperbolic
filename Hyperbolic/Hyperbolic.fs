module Hyperbolic.Draw

open System
open System.Drawing

open FsAlg.Generic

open Hyperbolic.Transformations

type Point = Point of float * float
type Radius = Radius of float
type Action = | DrawLine of Point * Point
              | DrawEuclideanCircle of Point * Radius
type Instructions = Action list
type Adjacency = Vertex | Edge | FirstLayer

type HyperbolicPattern =
    {   P: int
        Q: int
        Layers: int
        InitialPattern: Instructions }

type ImageFileProperties = 
        { ImageSize: int
          FileName: string
          BoundedCircleRadius: float 
          DrawTesselation: bool }

let blackPen = new Pen(Color.Black)

let transformPoint (transform : Matrix<float>) (Point(x, y)) = 
    let sumSquare = x*x + y*y
    let z = if sumSquare <> 1.0 
            then vector [2.0 * x/(1.0 - sumSquare);  2.0 * y/(1.0 - sumSquare); (1.0 + sumSquare)/ (1.0 - sumSquare)]
            else vector [2.0 * x; 2.0 * y; 1.0]
    let res = transform * z
    Point(res.[0]/(1.0 + res.[2]), res.[1]/(1.0 + res.[2]))

let transformAction transform action = 
    match action with
    | DrawLine (p1, p2) -> let p1Trans = transformPoint transform p1
                           let p2Trans = transformPoint transform p2
                           DrawLine (p1Trans, p2Trans)  
    | DrawEuclideanCircle (pt, r) -> 
        let ptTrans = transformPoint transform pt
        DrawEuclideanCircle (ptTrans, r)

// Hyperbolic line through two points equals Euclidean circle arc of circle passing through the two points
// and intersecting the Poincare bounding disk at right angles
// https://math.stackexchange.com/questions/1322444/how-to-construct-a-line-on-a-poincare-disk
// https://math.stackexchange.com/questions/213658/get-the-equation-of-a-circle-when-given-3-points/213678#213678
let getHyperbolicLine (Point(x1, x2))  (Point(y1, y2)) =    
    let square x y =  Math.Pow(x, 2.0) + Math.Pow(y, 2.0)
    let xSquare = square x1 x2
    let z1 = x1/xSquare
    let z2 = x2/xSquare
    let m  = matrix [[xSquare; x1; x2; 1.0]
                     [square y1 y2; y1; y2; 1.0]
                     [square z1 z2; z1; z2; 1.0]]
    let getCol c = Matrix.toVector m.[*, c]
    let m0 = Matrix.ofCols [getCol 1; getCol 2; getCol 3]
    let m1 = Matrix.ofCols [getCol 0; getCol 2; getCol 3]
    let m2 = Matrix.ofCols [getCol 0; getCol 1; getCol 3]
    
    let detM0 = (Matrix.det m0)
    let c1 = 0.5 * (Matrix.det m1) / detM0
    let c2 = -0.5 * (Matrix.det m2) / detM0
    let radius = Math.Sqrt(Math.Pow(x1 - c1, 2.0) + Math.Pow(x2 - c2, 2.0))
    
    let toDegrees angle = 
        let degrees = angle * 360.0 /(2.0 * Math.PI)
        if degrees < 0.0 then degrees + 360.0 else degrees

    let angle1 = Math.Atan2(x2 - c2, x1 - c1) |> toDegrees
    let angle2 = Math.Atan2(y2 - c2, y1 - c1) |> toDegrees
    let firstAngle = if angle1 < angle2 then angle1 else angle2
    let secondAngle = if angle1 < angle2 then angle2 else angle1
    let diff = secondAngle - firstAngle
    let sweepAngle = if diff > 180.0 then 360.0 - diff  else diff
    let startAngle = if diff > 180.0 then secondAngle else firstAngle
    (Point(c1, c2), radius, startAngle, sweepAngle)


let hyperbolicLineThroughOrigin (Point(x1, x2)) (Point(y1, y2)) = 
    Math.Abs(x1 * (y2 - x2) - (x2 * (y1 - x1))) < 0.000001;

let drawTransformation (graphics : Graphics) center radius (pen : Pen) (transform : Matrix<float>) (instructions : Instructions) = 
    let scale (Point (x, y)) = (x * radius + center, y * radius + center)
    
    instructions
    |> List.map (fun instr -> transformAction transform instr)
    |> List.iter (fun instr -> 
                    match instr with
                    | DrawLine (p1, p2) ->
                        if hyperbolicLineThroughOrigin p1 p2
                        then 
                            // hyperbolic line through origin equals euclidean line 
                            let p1x, p1y = scale p1
                            let p2x, p2y = scale p2
                            graphics.DrawLine(pen, PointF(float32 p1x, float32 p1y), PointF(float32 p2x, float32 p2y))
                        else 
                            let (c, r, startAngle, sweepAngle) = getHyperbolicLine p1 p2
                            let ptx, pty = scale c
                            let rectangle = RectangleF(
                                                PointF(float32 (ptx - r * radius), float32 (pty -  r * radius)), 
                                                SizeF(PointF(float32 (2.0 * r * radius), float32 (2.0 * r * radius))))     
                            graphics.DrawArc(pen, rectangle, float32 startAngle, float32 sweepAngle)
                    | DrawEuclideanCircle (pt, Radius r) ->   
                        let ptx, pty = scale pt
                        let rectangle = RectangleF(
                                            PointF(float32 (ptx - r * radius), float32 (pty - r * radius)), 
                                            SizeF(PointF(float32 (2.0 * r * radius), float32 (2.0 * r * radius))))
                        graphics.DrawEllipse(pen, rectangle) )

let drawHyperbolicPattern (hyperbolicPattern: HyperbolicPattern) (imageFileProperties: ImageFileProperties) =
    let p = hyperbolicPattern.P
    let q = hyperbolicPattern.Q
    let transformationMatrices = transformationMatrices p q
    let rotateP = transformationMatrices.RotateP
    let rotate2P = rotateP * rotateP
    let rotate3P = rotateP * rotate2P
    let rotateQ = transformationMatrices.RotateQ
    let size = imageFileProperties.ImageSize
    let center = (float) size / 2.0
    let radius = imageFileProperties.BoundedCircleRadius
    let layers = hyperbolicPattern.Layers
    let image = new Bitmap(size, size)
    let graphics = Graphics.FromImage image
    let instructions = hyperbolicPattern.InitialPattern
    let rec replicate graphics transform layers adjacency instructions = 
        let rec replicateAcrossVertices count rotateVertex = 
            if count = 0 then ()
            else 
                replicate graphics rotateVertex (layers - 1) Vertex instructions
                replicateAcrossVertices (count - 1) (rotateVertex * rotateQ)

        let rec replicateAcrossEdges count rotateCenter = 
            if count = 0 then ()
            else
                let rotateVertex = rotateCenter * rotateQ
                replicate graphics rotateVertex (layers - 1) Edge instructions
                replicateAcrossVertices (q - 3) (rotateVertex * rotateQ)
                replicateAcrossEdges (count - 1) (rotateCenter * rotateP)

        drawTransformation graphics center radius blackPen transform instructions
    
        if layers > 0 then
            match adjacency with
            | Edge -> replicateAcrossEdges (p - 3) (transform * rotate3P)
            | Vertex -> replicateAcrossEdges (p - 2) (transform * rotate2P)
            | FirstLayer -> replicateAcrossEdges p transform
    
    // draws bounding circle
    drawTransformation graphics center radius blackPen identity [DrawEuclideanCircle (Point(0.0, 0.0), Radius 1.0)]
    // draws hyperbolic pattern
    replicate graphics identity (layers - 1) FirstLayer instructions
    
    if imageFileProperties.DrawTesselation
    then 
        let fP = float p
        let linePen = new Pen(Color.Green)
        linePen.DashPattern <-[|float32 4.0; float32 4.0|]
        let circlePen = new Pen(Color.Purple)

        let tesselationLines = 
            [1.0 .. fP]
            |> List.map (fun n -> DrawEuclideanCircle(Point (Math.Sqrt(2.0)*Math.Cos(2.0*Math.PI*n/fP), Math.Sqrt(2.0)*Math.Sin(2.0*Math.PI*n/fP)), Radius 1.0))

        let helperLines  = 
            [1.0 .. fP] 
            |> List.map (fun n -> DrawLine(Point(Math.Cos(Math.PI*n/fP), Math.Sin(Math.PI*n/fP)),
                                           Point(Math.Cos(Math.PI*(n + fP)/fP), Math.Sin(Math.PI*(n + fP)/fP))))

        drawTransformation graphics center radius circlePen identity tesselationLines
        drawTransformation graphics center radius linePen identity helperLines
        drawTransformation graphics center radius blackPen identity instructions 

    image.RotateFlip RotateFlipType.RotateNoneFlipY  
    image.Save imageFileProperties.FileName