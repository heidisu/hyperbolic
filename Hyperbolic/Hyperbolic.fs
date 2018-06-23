module Hyperbolic.Draw

open System
open System.Drawing

open FsAlg.Generic

type Point = Point of float * float
type Radius = Radius of float
type Action = | DrawLine of Point * Point
              | DrawCircle of Point * Radius
type Instructions = Action list
type Adjacency = Vertex | Edge | FirstLayer

type ImageProperties = 
        { Size: int
          CircleRadius: float }

let imageProperties = { Size = 700
                        CircleRadius = 300.0 }
let [<Literal>] P = 6
let [<Literal>] Q = 4

let fP = float P
let fQ = float Q
let coshB = Math.Cos(Math.PI/fQ) / Math.Sin(Math.PI/fP)
let cosh2B = 2.0 * Math.Pow(coshB, 2.0) - 1.0
let sinh2B = Math.Sqrt(Math.Pow(cosh2B, 2.0)-1.0)

let identity = matrix [[1.0; 0.0; 0.0]
                       [0.0; 1.0; 0.0]
                       [0.0; 0.0; 1.0]]

let reflectEdgeBisector = matrix [[1.0; 0.0; 0.0]
                                  [0.0; -1.0; 0.0]
                                  [0.0; 0.0; 1.0]]

let reflectPgonEdge = matrix [[-cosh2B; 0.0;sinh2B]
                              [0.0; 1.0; 0.0]
                              [-sinh2B; 0.0; cosh2B]]

let reflectHypotenuse = matrix [[Math.Cos(2.0*Math.PI/fP); Math.Sin(2.0*Math.PI/fP); 0.0]
                                [Math.Sin(2.0*Math.PI/fP); -Math.Cos(2.0*Math.PI/fP); 0.0]
                                [0.0; 0.0; 1.0]]

let rotateP = reflectHypotenuse * reflectEdgeBisector
let rotate2P = rotateP * rotateP
let rotate3P = rotate2P * rotateP
let rotateQ = reflectPgonEdge * reflectHypotenuse

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
    | DrawCircle (pt, r) -> let ptTrans = transformPoint transform pt
                            DrawCircle (ptTrans, r)

let drawTransformation (graphics : Graphics) (pen : Pen) (transform : Matrix<float>) (instructions : Instructions) = 
    let center = (float) imageProperties.Size / 2.0
    let radius = imageProperties.CircleRadius
    let scale (Point (x, y)) = (x * radius + center, y * radius + center)
    
    instructions
    |> List.map (fun instr -> transformAction transform instr)
    |> List.iter (fun instr -> match instr with
                               | DrawLine (p1, p2) -> let p1x, p1y = scale p1
                                                      let p2x, p2y = scale p2
                                                      graphics.DrawLine(pen, new PointF(float32 p1x, float32 p1y), new PointF(float32 p2x, float32 p2y))
                               | DrawCircle (pt, Radius r) ->   let ptx, pty = scale pt
                                                                let rectangle = new RectangleF(
                                                                                        new PointF(float32 (ptx - r*radius), float32 (pty - r* radius)), 
                                                                                        new SizeF(PointF(float32 (2.0 * r * radius), float32 (2.0 * r * radius))))
                                                                graphics.DrawEllipse(pen, rectangle) )

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
            replicateAcrossVertices (Q - 3) (rotateVertex * rotateQ)
            replicateAcrossEdges (count - 1) (rotateCenter * rotateP)

    drawTransformation graphics blackPen transform instructions
    
    if layers > 0 then
        match adjacency with
        | Edge -> replicateAcrossEdges (P - 3) (transform * rotate3P)
        | Vertex -> replicateAcrossEdges (P - 2) (transform * rotate2P)
        | FirstLayer -> replicateAcrossEdges P transform


let drawInstructions filename (drawingFunctions : (Graphics -> unit) seq) = 
    let size = imageProperties.Size
    let image = new Bitmap(size, size)
    let graphics = Graphics.FromImage image

    drawingFunctions
    |> Seq.iter (fun drawingFunction -> drawingFunction graphics)
    image.RotateFlip RotateFlipType.RotateNoneFlipY  
    image.Save filename

let drawImage filename layers instructions = 
    let drawingFunctions = 
        [fun graphics -> drawTransformation graphics blackPen identity [DrawCircle (Point(0.0, 0.0), Radius 1.0)]
         fun graphics -> drawTransformation graphics blackPen identity instructions
         fun graphics -> replicate graphics identity (layers - 1) FirstLayer instructions]
    drawInstructions filename drawingFunctions
    
let drawImageWithTesselation filename transform instructions = 
    
    let linePen = new Pen(Color.Green)
    linePen.DashPattern <-[|float32 4.0; float32 4.0|]
    let circlePen = new Pen(Color.Purple)

    let tesselationLines = 
        [1.0 .. fP]
        |> List.map (fun n -> DrawCircle(Point (Math.Sqrt(2.0)*Math.Cos(2.0*Math.PI*n/fP), Math.Sqrt(2.0)*Math.Sin(2.0*Math.PI*n/fP)), Radius 1.0))

    let helperLines  = 
        [1.0 .. fP] 
        |> List.map (fun n -> DrawLine(Point(Math.Cos(Math.PI*n/fP), Math.Sin(Math.PI*n/fP)),
                                       Point(Math.Cos(Math.PI*(n + fP)/fP), Math.Sin(Math.PI*(n + fP)/fP))))

    let drawingFunctions = [
     fun graphics -> drawTransformation graphics blackPen identity [DrawCircle(Point(0.0, 0.0), Radius 1.0)]
     fun graphics -> drawTransformation graphics circlePen identity tesselationLines
     fun graphics -> drawTransformation graphics linePen identity helperLines
     fun graphics -> drawTransformation graphics blackPen identity instructions 
     fun graphics -> drawTransformation graphics blackPen transform instructions
    ]

    drawInstructions filename drawingFunctions