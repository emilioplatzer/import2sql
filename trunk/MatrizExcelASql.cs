/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 15/03/2008
 * Time: 01:51 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace TodoASql
{
	/// <summary>
	/// Description of MatrizExcelASql.
	/// </summary>
	public class MatrizExcelASql
	{
		ReceptorSql Receptor;
		public MatrizExcelASql(ReceptorSql receptor){
			this.Receptor=receptor;
		}
		public void PasarHoja(RangoExcel matriz,RangoExcel[] filas, RangoExcel[] columnas){
			int maxFila=matriz.CantidadFilas;
			int maxCol=matriz.CantidadColumnas;
		}
	}
}
