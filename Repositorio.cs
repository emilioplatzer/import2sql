/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 05/04/2008
 * Time: 12:48 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using TodoASql;

namespace Modelador
{
	/// <summary>
	/// Description of Repositorio.
	/// </summary>
	public class Repositorio
	{
		internal BaseDatos db;
		public Repositorio(BaseDatos db){
			this.db=db;
		}
	}
}
