

import { SVGProps } from "react";

export default function ChevronDown(props: SVGProps<SVGSVGElement>) {
  return (
    <>
      <svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" viewBox="0 0 24 24" {...props}>
        <path fill="currentColor" d="M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z" />
      </svg>
    </>
  );
}