module Hyperbolic.Transformations

open System
open FsAlg.Generic

type TransformationMatrices = 
    { ReflectEdgeBisector: Matrix<float>
      ReflectPgonEdge: Matrix<float>
      ReflectHypotenuse: Matrix<float>
      RotateP: Matrix<float>
      RotateQ: Matrix<float>}

let identity = matrix [[1.0; 0.0; 0.0]
                       [0.0; 1.0; 0.0]
                       [0.0; 0.0; 1.0]]

let transformationMatrices p q = 
    let fP = float p
    let fQ = float q
    let coshB = Math.Cos(Math.PI/fQ) / Math.Sin(Math.PI/fP)
    let cosh2B = 2.0 * Math.Pow(coshB, 2.0) - 1.0
    let sinh2B = Math.Sqrt(Math.Pow(cosh2B, 2.0)-1.0)

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
    let rotateQ = reflectPgonEdge * reflectHypotenuse
    { ReflectEdgeBisector = reflectEdgeBisector
      ReflectPgonEdge = reflectPgonEdge
      ReflectHypotenuse = reflectHypotenuse
      RotateP = rotateP
      RotateQ = rotateQ }